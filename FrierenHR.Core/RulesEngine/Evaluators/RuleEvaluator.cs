using FrierenHR.Core.RulesEngine.Evaluators;
using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Core.RulesEngine;

public class RuleEvaluator : IRuleEvaluator
{
    public RuleEvaluationResult Evaluate(RuleDefinition rule, RuleContext context)
    {
        if (rule is null) return RuleEvaluationResult.Failed("Rule definition is null.");
        if (rule.Conditions.Count == 0 && string.IsNullOrEmpty(rule.Action.Type))
            return RuleEvaluationResult.Failed("Rule has no conditions and no action.");

        bool allMatch;
        try { allMatch = ConditionEvaluator.AllMatch(rule.Conditions, context, out var err); if (err is not null) return RuleEvaluationResult.Failed(err); }
        catch (InvalidOperationException ex) { return RuleEvaluationResult.Failed(ex.Message); }

        return !allMatch ? RuleEvaluationResult.NotMatched() : ActionExecutor.Execute(rule.Action, context);
    }

    public RuleEvaluationResult EvaluateFirstMatch(IEnumerable<RuleDefinition> rules, string ruleType, RuleContext context)
    {
        var candidates = rules.Where(r => string.Equals(r.RuleType, ruleType, StringComparison.OrdinalIgnoreCase));
        RuleEvaluationResult? lastFailure = null;
        foreach (var rule in candidates)
        {
            var result = Evaluate(rule, context);
            if (result.Matched) return result;
            if (!result.Success) lastFailure = result;
        }
        return lastFailure ?? RuleEvaluationResult.NotMatched();
    }
}