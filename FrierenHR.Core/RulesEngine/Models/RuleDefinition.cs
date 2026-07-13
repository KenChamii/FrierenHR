using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Core.RulesEngine;

// { "ruleType": "LeaveAccrual", "conditions": [{ "field": "tenureMonths", "op": ">=", "value": 12 }], "action": { "type": "AccrueDays", "amount": 1.25, "cap": 15 } }
public class RuleDefinition
{
    public string RuleType { get; set; } = string.Empty;
    public List<RuleCondition> Conditions { get; set; } = new();
    public RuleAction Action { get; set; } = new();
}