using System.Text.Json;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine;
using FrierenHR.Core.RulesEngine.Schema;

namespace FrierenHR.Application.Features.RulesConfig;

public class RuleConfigService : IRuleConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRuleConfigRepository _ruleConfigRepository;
    private readonly IRuleEvaluator _ruleEvaluator;

    public RuleConfigService(IRuleConfigRepository ruleConfigRepository, IRuleEvaluator ruleEvaluator)
    {
        _ruleConfigRepository = ruleConfigRepository;
        _ruleEvaluator = ruleEvaluator;
    }

    /// <summary>What a valid rule looks like per rule type. The editing UI builds its form from this.</summary>
    public IReadOnlyDictionary<RuleType, RuleTypeSchema> GetSchema() => RuleSchemaCatalog.All;

    public async Task<RuleConfigDto> SaveAsync(SaveRuleConfigDto dto, CancellationToken ct = default)
    {
        RuleDefinition? parsed;
        try { parsed = JsonSerializer.Deserialize<RuleDefinition>(dto.RuleJson, JsonOptions); }
        catch (JsonException ex) { throw new InvalidOperationException($"RuleJson is not valid JSON: {ex.Message}"); }

        if (parsed is null)
            throw new InvalidOperationException("RuleJson did not describe a rule.");

        // Valid JSON is not a valid rule. The schema catalog is the same one the form is built
        // from, so a mistyped field or an unsupported operator is rejected here by name rather
        // than silently never matching at evaluation time.
        var problem = RuleSchemaCatalog.Validate(dto.RuleType, parsed);
        if (problem is not null) throw new InvalidOperationException(problem);

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
            // Anything unparseable would already have failed SaveAsync on the way in. Skip
            // rather than failing the whole test run.
            if (parsed is not null) rules.Add(parsed);
        }

        var context = new RuleContext();
        foreach (var (field, rawValue) in dto.Facts)
            context.Set(field, NormalizeFactValue(rawValue));

        var result = _ruleEvaluator.EvaluateFirstMatch(rules, dto.RuleType.ToString(), context);
        return new RuleEvaluationResultDto(result.Matched, result.Success, result.ResultValue, result.Message, result.ActionType);
    }

    // Facts arrive as untyped JSON, so model binding gives JsonElement rather than int or
    // string. ConditionEvaluator cannot read a JsonElement, so unwrap to CLR types first.
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

    /// <summary>
    /// Fills in the catalog's starter rule for any rule type this company has not configured.
    /// Never overwrites an existing config, so it is safe to call again after a new rule type
    /// is added to the enum.
    /// </summary>
    public async Task<List<RuleConfigDto>> SeedDefaultsAsync(Guid companyId, CancellationToken ct = default)
    {
        var existing = await _ruleConfigRepository.GetByCompanyAsync(companyId, ct);
        var existingTypes = existing.Select(c => c.RuleType).ToHashSet();

        foreach (var (ruleType, schema) in RuleSchemaCatalog.All)
        {
            if (existingTypes.Contains(ruleType)) continue;
            var entity = new CompanyRuleConfig
            {
                CompanyId = companyId,
                RuleType = ruleType,
                RuleJson = JsonSerializer.Serialize(schema.Starter, JsonOptions),
                IsActive = true,
                Priority = 0
            };
            await _ruleConfigRepository.AddAsync(entity, ct);
        }
        await _ruleConfigRepository.SaveChangesAsync(ct);

        return await GetByCompanyAsync(companyId, ct);
    }
}
