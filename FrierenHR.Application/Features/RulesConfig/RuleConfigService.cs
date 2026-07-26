using System.Text.Json;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.RulesEngine;

namespace FrierenHR.Application.Features.RulesConfig;

public class RuleConfigService : IRuleConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Starter templates so a brand-new company doesn't face a blank JSON box with no idea
    // what a valid rule looks like. "Seed Defaults" writes these in as real, active configs;
    // the frontend also offers loading one into the editor without saving, for someone who
    // wants to start from a template and tweak it before committing.
    public static readonly IReadOnlyDictionary<FrierenHR.Core.Enums.RuleType, string> DefaultTemplates =
        new Dictionary<FrierenHR.Core.Enums.RuleType, string>
        {
            [FrierenHR.Core.Enums.RuleType.LeaveAccrual] =
                "{\"ruleType\":\"LeaveAccrual\",\"conditions\":[{\"field\":\"tenureMonths\",\"op\":\">=\",\"value\":0}],\"action\":{\"type\":\"AccrueDays\",\"amount\":1.25,\"cap\":15}}",
            [FrierenHR.Core.Enums.RuleType.LeaveApproval] =
                "{\"ruleType\":\"LeaveApproval\",\"conditions\":[{\"field\":\"days\",\"op\":\"<=\",\"value\":2}],\"action\":{\"type\":\"AutoApprove\"}}",
            [FrierenHR.Core.Enums.RuleType.OTMultiplier] =
                "{\"ruleType\":\"OTMultiplier\",\"conditions\":[{\"field\":\"otHours\",\"op\":\">=\",\"value\":0}],\"action\":{\"type\":\"OTMultiplier\",\"amount\":1.5}}",
            [FrierenHR.Core.Enums.RuleType.LateGracePeriod] =
                "{\"ruleType\":\"LateGracePeriod\",\"conditions\":[{\"field\":\"clockInHour\",\"op\":\">=\",\"value\":9}],\"action\":{\"type\":\"LateGraceMinutes\",\"amount\":15}}",
            [FrierenHR.Core.Enums.RuleType.ApprovalEscalation] =
                "{\"ruleType\":\"ApprovalEscalation\",\"conditions\":[{\"field\":\"daysWaiting\",\"op\":\">=\",\"value\":3}],\"action\":{\"type\":\"EscalateAfterDays\",\"amount\":3}}",
        };

    private readonly IRuleConfigRepository _ruleConfigRepository;
    private readonly IRuleEvaluator _ruleEvaluator;

    public RuleConfigService(IRuleConfigRepository ruleConfigRepository, IRuleEvaluator ruleEvaluator)
    {
        _ruleConfigRepository = ruleConfigRepository;
        _ruleEvaluator = ruleEvaluator;
    }

    public async Task<RuleConfigDto> SaveAsync(SaveRuleConfigDto dto, CancellationToken ct = default)
    {
        RuleDefinition? parsed;
        try { parsed = JsonSerializer.Deserialize<RuleDefinition>(dto.RuleJson, JsonOptions); }
        catch (JsonException ex) { throw new InvalidOperationException($"RuleJson is not valid JSON: {ex.Message}"); }

        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Action.Type))
            throw new InvalidOperationException("RuleJson must include a non-empty 'action.type'.");

        if (dto.Id is not null)
        {
            var existing = await _ruleConfigRepository.GetByIdAsync(dto.Id.Value, ct)
                ?? throw new InvalidOperationException($"CompanyRuleConfig '{dto.Id}' not found.");
            if (existing.CompanyId != dto.CompanyId)
                throw new InvalidOperationException("Cannot move a rule config to a different company.");

            existing.RuleType = dto.RuleType;
            existing.RuleJson = dto.RuleJson;
            existing.IsActive = dto.IsActive;
            existing.Priority = dto.Priority;
            existing.UpdatedAt = DateTime.UtcNow;

            _ruleConfigRepository.Update(existing);
            await _ruleConfigRepository.SaveChangesAsync(ct);
            return ToDto(existing);
        }

        var entity = new CompanyRuleConfig
        {
            CompanyId = dto.CompanyId,
            RuleType = dto.RuleType,
            RuleJson = dto.RuleJson,
            IsActive = dto.IsActive,
            Priority = dto.Priority
        };
        await _ruleConfigRepository.AddAsync(entity, ct);
        await _ruleConfigRepository.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<RuleEvaluationResultDto> TestEvaluateAsync(TestEvaluateRequestDto dto, CancellationToken ct = default)
    {
        var configs = await _ruleConfigRepository.GetActiveRulesAsync(dto.CompanyId, dto.RuleType, ct);

        var rules = new List<RuleDefinition>();
        foreach (var config in configs)
        {
            var parsed = JsonSerializer.Deserialize<RuleDefinition>(config.RuleJson, JsonOptions);
            // A row that fails to deserialize here would already have failed SaveAsync's
            // validation on the way in — skip defensively instead of 500ing the whole test.
            if (parsed is not null) rules.Add(parsed);
        }

        var context = new RuleContext();
        foreach (var (field, rawValue) in dto.Facts)
            context.Set(field, NormalizeFactValue(rawValue));

        var result = _ruleEvaluator.EvaluateFirstMatch(rules, dto.RuleType.ToString(), context);
        return new RuleEvaluationResultDto(result.Matched, result.Success, result.ResultValue, result.Message, result.ActionType);
    }

    // Facts arrive over HTTP as untyped JSON, so ASP.NET Core model-binds
    // Dictionary<string, object?> values as JsonElement — not int/decimal/string.
    // ConditionEvaluator.TryToDecimal has no idea what a JsonElement is. This is
    // the exact ambiguity Appendix D calls out; here's where it's actually resolved,
    // by unwrapping to plain CLR types before the fact ever reaches RuleContext.
    private static object? NormalizeFactValue(object? raw)
    {
        if (raw is not JsonElement element) return raw;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(e => NormalizeFactValue(e)).ToList(),
            _ => null
        };
    }

    private static RuleConfigDto ToDto(CompanyRuleConfig c) => new(c.Id, c.CompanyId, c.RuleType, c.RuleJson, c.IsActive, c.Priority);

    public async Task<List<RuleConfigDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default)
    {
        var configs = await _ruleConfigRepository.GetByCompanyAsync(companyId, ct);
        return configs.Select(ToDto).ToList();
    }

    // Fills in a starter rule for any RuleType this company doesn't already have configured.
    // Never overwrites an existing config — safe to call more than once (e.g. after a new
    // RuleType gets added to the enum later, this backfills just the missing one).
    public async Task<List<RuleConfigDto>> SeedDefaultsAsync(Guid companyId, CancellationToken ct = default)
    {
        var existing = await _ruleConfigRepository.GetByCompanyAsync(companyId, ct);
        var existingTypes = existing.Select(c => c.RuleType).ToHashSet();

        foreach (var (ruleType, json) in DefaultTemplates)
        {
            if (existingTypes.Contains(ruleType)) continue;
            var entity = new CompanyRuleConfig { CompanyId = companyId, RuleType = ruleType, RuleJson = json, IsActive = true, Priority = 0 };
            await _ruleConfigRepository.AddAsync(entity, ct);
        }
        await _ruleConfigRepository.SaveChangesAsync(ct);

        return await GetByCompanyAsync(companyId, ct);
    }
}