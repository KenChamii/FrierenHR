using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine;
using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Tests.RulesEngine;

public class RuleEvaluatorTests
{
    private readonly RuleEvaluator _evaluator = new();

    private static RuleDefinition Accrual(decimal amount, int minTenure, decimal? cap = null) => new()
    {
        RuleType = nameof(RuleType.LeaveAccrual),
        Conditions = [new RuleCondition { Field = "tenureMonths", Op = ">=", Value = minTenure }],
        Action = new RuleAction { Type = "AccrueDays", Amount = amount, Cap = cap }
    };

    [Fact]
    public void A_matching_rule_runs_its_action()
    {
        var result = _evaluator.Evaluate(Accrual(1.25m, 12), new RuleContext().Set("tenureMonths", 24));

        Assert.True(result.Matched);
        Assert.Equal(1.25m, result.ResultValue);
    }

    [Fact]
    public void A_rule_whose_conditions_fail_does_not_run_its_action()
    {
        var result = _evaluator.Evaluate(Accrual(1.25m, 12), new RuleContext().Set("tenureMonths", 6));

        Assert.False(result.Matched);
        Assert.True(result.Success);
        Assert.Null(result.ResultValue);
    }

    [Fact]
    public void A_null_rule_fails_rather_than_throwing()
    {
        var result = _evaluator.Evaluate(null!, new RuleContext());

        Assert.False(result.Success);
    }

    [Fact]
    public void A_rule_with_neither_conditions_nor_an_action_fails()
    {
        var result = _evaluator.Evaluate(new RuleDefinition(), new RuleContext());

        Assert.False(result.Success);
    }

    [Fact]
    public void A_rule_with_no_conditions_but_an_action_always_matches()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Action = new RuleAction { Type = "AccrueDays", Amount = 1m }
        };

        Assert.True(_evaluator.Evaluate(rule, new RuleContext()).Matched);
    }

    [Fact]
    public void A_condition_error_surfaces_as_a_failure_on_a_single_rule()
    {
        var rule = Accrual(1.25m, 12);
        rule.Conditions[0].Field = "tenureMonth"; // typo

        var result = _evaluator.Evaluate(rule, new RuleContext().Set("tenureMonths", 24));

        Assert.False(result.Success);
        Assert.Contains("tenureMonth", result.Message);
    }

    [Fact]
    public void A_non_numeric_comparison_is_caught_and_reported_rather_than_thrown()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveApproval),
            Conditions = [new RuleCondition { Field = "leaveType", Op = ">=", Value = "Vacation" }],
            Action = new RuleAction { Type = "AutoApprove" }
        };

        var result = _evaluator.Evaluate(rule, new RuleContext().Set("leaveType", "Sick"));

        Assert.False(result.Success);
    }

    [Fact]
    public void EvaluateFirstMatch_takes_the_first_matching_rule_in_the_order_given()
    {
        // Callers pass rules ordered by Priority, so the first match is the highest priority.
        // This is how an exception for long-tenured staff sits above the general rule.
        var rules = new[] { Accrual(2.0m, 60), Accrual(1.25m, 0) };

        var senior = _evaluator.EvaluateFirstMatch(rules, nameof(RuleType.LeaveAccrual), new RuleContext().Set("tenureMonths", 72));
        var junior = _evaluator.EvaluateFirstMatch(rules, nameof(RuleType.LeaveAccrual), new RuleContext().Set("tenureMonths", 6));

        Assert.Equal(2.0m, senior.ResultValue);
        Assert.Equal(1.25m, junior.ResultValue);
    }

    [Fact]
    public void EvaluateFirstMatch_ignores_rules_of_other_types()
    {
        var otRule = new RuleDefinition
        {
            RuleType = nameof(RuleType.OTMultiplier),
            Action = new RuleAction { Type = "OTMultiplier", Amount = 1.5m }
        };

        var result = _evaluator.EvaluateFirstMatch([otRule], nameof(RuleType.LeaveAccrual), new RuleContext());

        Assert.False(result.Matched);
    }

    [Fact]
    public void EvaluateFirstMatch_compares_the_rule_type_without_regard_to_case()
    {
        var result = _evaluator.EvaluateFirstMatch([Accrual(1.25m, 0)], "leaveaccrual", new RuleContext().Set("tenureMonths", 1));

        Assert.True(result.Matched);
    }

    [Fact]
    public void EvaluateFirstMatch_returns_not_matched_when_nothing_is_configured()
    {
        var result = _evaluator.EvaluateFirstMatch([], nameof(RuleType.LeaveAccrual), new RuleContext());

        Assert.False(result.Matched);
        Assert.True(result.Success);
    }

    [Fact]
    public void A_broken_rule_does_not_stop_a_later_one_from_matching()
    {
        var broken = Accrual(2.0m, 60);
        broken.Conditions[0].Field = "doesNotExist";

        var result = _evaluator.EvaluateFirstMatch([broken, Accrual(1.25m, 0)], nameof(RuleType.LeaveAccrual),
            new RuleContext().Set("tenureMonths", 72));

        Assert.True(result.Matched);
        Assert.Equal(1.25m, result.ResultValue);
    }

    [Fact]
    public void When_every_rule_is_broken_the_last_failure_is_reported()
    {
        // Current behaviour, pinned deliberately: a mistyped field is reported only when no
        // other rule matches. If a later rule matches, the mistake is invisible. Changing this
        // is a decision, not a bug fix, so the test records what the engine does today.
        var broken = Accrual(2.0m, 60);
        broken.Conditions[0].Field = "doesNotExist";

        var result = _evaluator.EvaluateFirstMatch([broken], nameof(RuleType.LeaveAccrual),
            new RuleContext().Set("tenureMonths", 72));

        Assert.False(result.Matched);
        Assert.False(result.Success);
        Assert.Contains("doesNotExist", result.Message);
    }
}
