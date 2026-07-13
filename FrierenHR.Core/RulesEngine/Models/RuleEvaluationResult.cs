namespace FrierenHR.Core.RulesEngine.Models;

public class RuleEvaluationResult
{
    public bool Matched { get; set; }
    public bool Success { get; set; } = true;
    public decimal? ResultValue { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ActionType { get; set; } // e.g. "AutoApprove" — lets callers branch on the actual
                                            // action instead of parsing the human-readable Message

    public static RuleEvaluationResult NotMatched() => new() { Matched = false, Success = true, Message = "Conditions not met." };
    public static RuleEvaluationResult Failed(string message) => new() { Matched = false, Success = false, Message = message };
}