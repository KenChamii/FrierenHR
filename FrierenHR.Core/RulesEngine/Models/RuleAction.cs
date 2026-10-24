namespace FrierenHR.Core.RulesEngine.Models;

// Fixed action-type catalog.
public class RuleAction
{
    public string Type { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public decimal? Cap { get; set; }
    public Dictionary<string, object>? ExtraParams { get; set; }
}