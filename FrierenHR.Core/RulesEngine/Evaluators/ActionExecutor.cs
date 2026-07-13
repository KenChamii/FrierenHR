using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Core.RulesEngine.Evaluators;

public static class ActionExecutor
{
    public static RuleEvaluationResult Execute(RuleAction action, RuleContext context) => action.Type switch
    {
        "AccrueDays" => ExecuteAccrueDays(action, context),
        "RequireApproval" => Ok(action.Type, 1, "Approval required by rule."),
        "AutoApprove" => Ok(action.Type, 1, "Auto-approved by rule."),
        "OTMultiplier" => ExecuteOtMultiplier(action, context),
        "LateGraceMinutes" => Ok(action.Type, action.Amount ?? 0, $"Grace period: {action.Amount ?? 0} minutes."),
        "EscalateAfterDays" => Ok(action.Type, action.Amount ?? 0, $"Escalate after {action.Amount ?? 0} day(s)."),
        _ => RuleEvaluationResult.Failed($"Unsupported action type '{action.Type}'.")
    };

    private static RuleEvaluationResult ExecuteAccrueDays(RuleAction action, RuleContext context)
    {
        if (action.Amount is null) return RuleEvaluationResult.Failed("AccrueDays action requires 'amount'.");
        var accrued = action.Amount.Value;
        if (action.Cap is not null && context.TryGet("currentBalance", out var raw) && raw is not null
            && decimal.TryParse(raw.ToString(), out var currentBalance))
        {
            var projected = currentBalance + accrued;
            if (projected > action.Cap.Value) accrued = Math.Max(0, action.Cap.Value - currentBalance);
        }
        return new RuleEvaluationResult { Matched = true, Success = true, ResultValue = accrued, Message = $"Accrued {accrued} day(s).", ActionType = action.Type };
    }

    private static RuleEvaluationResult ExecuteOtMultiplier(RuleAction action, RuleContext context)
    {
        if (action.Amount is null) return RuleEvaluationResult.Failed("OTMultiplier action requires 'amount'.");
        decimal otHours = 0;
        if (context.TryGet("otHours", out var raw) && raw is not null) decimal.TryParse(raw.ToString(), out otHours);
        var payableHours = otHours * action.Amount.Value;
        return new RuleEvaluationResult { Matched = true, Success = true, ResultValue = payableHours, Message = $"{otHours}h OT x{action.Amount.Value} = {payableHours}h payable.", ActionType = action.Type };
    }

    private static RuleEvaluationResult Ok(string actionType, decimal value, string message) =>
        new() { Matched = true, Success = true, ResultValue = value, Message = message, ActionType = actionType };
}