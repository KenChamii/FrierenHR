using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Core.RulesEngine;

public interface IRuleEvaluator
{
    RuleEvaluationResult Evaluate(RuleDefinition rule, RuleContext context);
    RuleEvaluationResult EvaluateFirstMatch(IEnumerable<RuleDefinition> rules, string ruleType, RuleContext context);
}