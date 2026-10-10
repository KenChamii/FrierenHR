using FrierenHR.Core.RulesEngine;
using FrierenHR.Core.RulesEngine.Evaluators;
using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Tests.RulesEngine;

public class ConditionEvaluatorTests
{
    private static RuleContext Context(string field, object? value) => new RuleContext().Set(field, value);

    private static RuleCondition Condition(string field, string op, object? value) =>
        new() { Field = field, Op = op, Value = value };

    [Theory]
    [InlineData(">=", 12, 12, true)]
    [InlineData(">=", 12, 11, false)]
    [InlineData("<=", 2, 2, true)]
    [InlineData("<=", 2, 3, false)]
    [InlineData("==", 5, 5, true)]
    [InlineData("==", 5, 6, false)]
    [InlineData("!=", 5, 6, true)]
    [InlineData("!=", 5, 5, false)]
    public void Numeric_operators_compare_the_fact_against_the_rule_value(string op, int ruleValue, int fact, bool expected)
    {
        var matched = ConditionEvaluator.Matches(Condition("tenureMonths", op, ruleValue), Context("tenureMonths", fact), out var error);

        Assert.Null(error);
        Assert.Equal(expected, matched);
    }

    [Fact]
    public void Numeric_comparison_works_across_mixed_numeric_types()
    {
        // The rule value arrives from JSON and the fact from the domain, so one is often a
        // double and the other a decimal. Both go through TryToDecimal.
        var matched = ConditionEvaluator.Matches(Condition("days", "<=", 2.5d), Context("days", 2.0m), out var error);

        Assert.Null(error);
        Assert.True(matched);
    }

    [Fact]
    public void Numeric_comparison_parses_a_numeric_string()
    {
        var matched = ConditionEvaluator.Matches(Condition("otHours", ">=", "1.5"), Context("otHours", 2m), out var error);

        Assert.Null(error);
        Assert.True(matched);
    }

    [Theory]
    [InlineData("Vacation", true)]
    [InlineData("vacation", true)]
    [InlineData("Sick", false)]
    public void Equality_on_text_ignores_case(string fact, bool expected)
    {
        var matched = ConditionEvaluator.Matches(Condition("leaveType", "==", "Vacation"), Context("leaveType", fact), out var error);

        Assert.Null(error);
        Assert.Equal(expected, matched);
    }

    [Fact]
    public void In_matches_any_value_of_a_comma_separated_list()
    {
        var condition = Condition("leaveType", "in", "Vacation, Sick");

        Assert.True(ConditionEvaluator.Matches(condition, Context("leaveType", "Sick"), out _));
        Assert.False(ConditionEvaluator.Matches(condition, Context("leaveType", "Unpaid"), out _));
    }

    [Fact]
    public void In_matches_any_value_of_a_list()
    {
        var condition = Condition("leaveType", "in", new List<object> { "Vacation", "Sick" });

        Assert.True(ConditionEvaluator.Matches(condition, Context("leaveType", "Vacation"), out _));
    }

    [Fact]
    public void An_unsupported_operator_is_an_error_not_a_non_match()
    {
        var matched = ConditionEvaluator.Matches(Condition("tenureMonths", ">", 12), Context("tenureMonths", 24), out var error);

        Assert.False(matched);
        Assert.NotNull(error);
        Assert.Contains("'>'", error);
    }

    [Fact]
    public void A_field_missing_from_the_context_is_an_error()
    {
        // Worth pinning: this is an error rather than a non-match, and EvaluateFirstMatch
        // treats it as a skip. A mistyped field name therefore falls through to the next rule
        // instead of surfacing. See ApprovalEscalation note in RuleSchemaCatalog.
        var matched = ConditionEvaluator.Matches(Condition("tenureMonth", ">=", 12), Context("tenureMonths", 24), out var error);

        Assert.False(matched);
        Assert.NotNull(error);
        Assert.Contains("tenureMonth", error);
    }

    [Fact]
    public void A_null_fact_is_treated_as_missing()
    {
        var matched = ConditionEvaluator.Matches(Condition("tenureMonths", ">=", 12), Context("tenureMonths", null), out var error);

        Assert.False(matched);
        Assert.NotNull(error);
    }

    [Fact]
    public void An_ordering_operator_on_a_non_numeric_field_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ConditionEvaluator.Matches(Condition("leaveType", ">=", "Vacation"), Context("leaveType", "Sick"), out _));
    }

    [Fact]
    public void AllMatch_requires_every_condition()
    {
        var context = new RuleContext().Set("tenureMonths", 24).Set("days", 3m);
        var conditions = new[]
        {
            Condition("tenureMonths", ">=", 12),
            Condition("days", "<=", 2),
        };

        Assert.False(ConditionEvaluator.AllMatch(conditions, context, out var error));
        Assert.Null(error); // a non-match, not a failure
    }

    [Fact]
    public void AllMatch_passes_when_every_condition_holds()
    {
        var context = new RuleContext().Set("tenureMonths", 24).Set("days", 2m);
        var conditions = new[]
        {
            Condition("tenureMonths", ">=", 12),
            Condition("days", "<=", 2),
        };

        Assert.True(ConditionEvaluator.AllMatch(conditions, context, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void AllMatch_on_no_conditions_is_vacuously_true()
    {
        Assert.True(ConditionEvaluator.AllMatch([], new RuleContext(), out _));
    }

    [Fact]
    public void AllMatch_stops_at_the_first_failing_condition()
    {
        // The second condition names a missing field. It must not be reached, so no error.
        var conditions = new[]
        {
            Condition("tenureMonths", ">=", 99),
            Condition("doesNotExist", ">=", 1),
        };

        Assert.False(ConditionEvaluator.AllMatch(conditions, Context("tenureMonths", 24), out var error));
        Assert.Null(error);
    }
}
