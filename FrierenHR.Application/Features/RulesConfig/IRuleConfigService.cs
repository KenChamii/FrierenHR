using FrierenHR.Application.Common.DTOs;
using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine.Schema;

namespace FrierenHR.Application.Features.RulesConfig;

public interface IRuleConfigService
{
    /// <summary>What a valid rule looks like per rule type. Served to the editing UI.</summary>
    IReadOnlyDictionary<RuleType, RuleTypeSchema> GetSchema();

    Task<RuleConfigDto> SaveAsync(SaveRuleConfigDto dto, CancellationToken ct = default);
    Task<RuleEvaluationResultDto> TestEvaluateAsync(TestEvaluateRequestDto dto, CancellationToken ct = default);
    Task<List<RuleConfigDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<List<RuleConfigDto>> SeedDefaultsAsync(Guid companyId, CancellationToken ct = default);
}
