using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

public record RuleConfigDto(Guid Id, Guid CompanyId, RuleType RuleType, string RuleJson, bool IsActive, int Priority);

// Id is null on create, set on update — SaveAsync branches on it.
public record SaveRuleConfigDto(Guid? Id, Guid CompanyId, RuleType RuleType, string RuleJson, bool IsActive, int Priority);

public record TestEvaluateRequestDto(Guid CompanyId, RuleType RuleType, Dictionary<string, object?> Facts);

public record RuleEvaluationResultDto(bool Matched, bool Success, decimal? ResultValue, string Message, string? ActionType);