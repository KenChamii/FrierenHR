namespace FrierenHR.Core.RulesEngine.Models;

// Fixed operator set on purpose: >=, <=, ==, !=, in.
public class RuleCondition
{
    public string Field { get; set; } = string.Empty;   // looked up in RuleContext, e.g. "tenureMonths"
    public string Op { get; set; } = string.Empty;
    public object? Value { get; set; }                  // stored loose so it works with any JSON deserializer
}