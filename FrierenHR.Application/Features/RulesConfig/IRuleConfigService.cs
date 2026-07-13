using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.RulesConfig;

public interface IRuleConfigService
{
    Task<RuleConfigDto> SaveAsync(SaveRuleConfigDto dto, CancellationToken ct = default);
    Task<RuleEvaluationResultDto> TestEvaluateAsync(TestEvaluateRequestDto dto, CancellationToken ct = default);
}