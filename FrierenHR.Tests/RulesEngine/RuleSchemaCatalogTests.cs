using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine;
using FrierenHR.Core.RulesEngine.Evaluators;
using FrierenHR.Core.RulesEngine.Models;
using FrierenHR.Core.RulesEngine.Schema;

namespace FrierenHR.Tests.RulesEngine;

/// <summary>The catalog claims to describe the engine.</summary>
public class RuleSchemaCatalogTests
{
    public static TheoryData<RuleType> AllRuleTypes()
    {
        var data = new TheoryData<RuleType>();
        foreach (var ruleType in Enum.GetValues<RuleType>()) data.Add(ruleType);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllRuleTypes))]
    public void Every_rule_type_has_a_schema(RuleType ruleType)
    {
        Assert.True(RuleSchemaCatalog.All.ContainsKey(ruleType));
    }

    [Theory]
    [MemberData(nameof(AllRuleTypes))]
    public void Every_schema_offers_at_least_one_field_and_one_action(RuleType ruleType)
    {
        var schema = RuleSchemaCatalog.For(ruleType);

        Assert.NotEmpty(schema.Fields);
        Assert.NotEmpty(schema.Actions);
    }

    [Theory]
    [MemberData(nameof(AllRuleTypes))]
    public void Every_schema_only_offers_operators_the_evaluator_implements(RuleType ruleType)
    {
        string[] supported = [">=", "<=", "==", "!=", "in"];


        foreach (var field in RuleSchemaCatalog.For(ruleType).Fields)
            Assert.All(field.Operators, op => Assert.Contains(op, supported));
    }

    [Theory]
    [MemberData(nameof(AllRuleTypes))]
    public void Every_schema_only_offers_actions_the_executor_implements(RuleType ruleType)
    {
        foreach (var action in RuleSchemaCatalog.For(ruleType).Actions)
        {
            var filled = new RuleAction
            {
                Type = action.Type,
                Amount = action.Parameters.Any(p => p.Name == "amount") ? 1m : null,
                Cap = action.Parameters.Any(p => p.Name == "cap") ? 10m : null
            };

            var result = ActionExecutor.Execute(filled, new RuleContext());

            Assert.True(result.Success, $"{ruleType}.{action.Type} is offered by the schema but the executor rejected it: {result.Message}");
        }
    }

    [Theory]
    [MemberData(nameof(AllRuleTypes))]
    public void Every_starter_rule_is_valid_against_its_own_schema(RuleType ruleType)
    {
        var schema = RuleSchemaCatalog.For(ruleType);

        Assert.Null(RuleSchemaCatalog.Validate(ruleType, schema.Starter));
    }

    [Theory]
    [MemberData(nameof(AllRuleTypes))]
    public void Every_starter_rule_actually_runs(RuleType ruleType)
    {
        // Seeding writes these in as live configs, so a starter that cannot evaluate would leave a company with a rule that never fires.
        var schema = RuleSchemaCatalog.For(ruleType);
        var context = new RuleContext();
        foreach (var field in schema.Fields) context.Set(field.Name, SampleFor(field));

        var result = new RuleEvaluator().Evaluate(schema.Starter, context);

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public void A_boolean_field_only_accepts_true_or_false()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.OTMultiplier),
            Conditions = [new RuleCondition { Field = "isHoliday", Op = "==", Value = "maybe" }],
            Action = new RuleAction { Type = "OTMultiplier", Amount = 2m }
        };

        var problem = RuleSchemaCatalog.Validate(RuleType.OTMultiplier, rule);

        Assert.NotNull(problem);
        Assert.Contains("true or false", problem);
    }

    [Fact]
    public void Holiday_overtime_can_be_expressed_without_changing_the_engine()
    {
        // The reason the holiday facts were added: a company sets a premium for overtime on a regular holiday as configuration.
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.OTMultiplier),
            Conditions =
            [
                new RuleCondition { Field = "isHoliday", Op = "==", Value = true },
                new RuleCondition { Field = "holidayType", Op = "==", Value = nameof(HolidayType.Regular) },
            ],
            Action = new RuleAction { Type = "OTMultiplier", Amount = 2m }
        };

        Assert.Null(RuleSchemaCatalog.Validate(RuleType.OTMultiplier, rule));

        var context = new RuleContext()
            .Set("otHours", 3m)
            .Set("isHoliday", true)
            .Set("holidayType", nameof(HolidayType.Regular));

        var result = new RuleEvaluator().Evaluate(rule, context);

        Assert.True(result.Matched);
        Assert.Equal(6m, result.ResultValue);
    }

    [Fact]
    public void An_ordinary_day_does_not_match_a_holiday_rule()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.OTMultiplier),
            Conditions = [new RuleCondition { Field = "isHoliday", Op = "==", Value = true }],
            Action = new RuleAction { Type = "OTMultiplier", Amount = 2m }
        };

        var context = new RuleContext().Set("otHours", 3m).Set("isHoliday", false);

        Assert.False(new RuleEvaluator().Evaluate(rule, context).Matched);
    }

    [Fact]
    public void An_unknown_field_is_rejected_by_name()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Conditions = [new RuleCondition { Field = "tenureMonth", Op = ">=", Value = 12 }],
            Action = new RuleAction { Type = "AccrueDays", Amount = 1.25m }
        };

        var problem = RuleSchemaCatalog.Validate(RuleType.LeaveAccrual, rule);

        Assert.NotNull(problem);
        Assert.Contains("tenureMonth", problem);
    }

    [Fact]
    public void An_operator_the_field_does_not_allow_is_rejected()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Conditions = [new RuleCondition { Field = "tenureMonths", Op = "in", Value = "12" }],
            Action = new RuleAction { Type = "AccrueDays", Amount = 1.25m }
        };

        Assert.NotNull(RuleSchemaCatalog.Validate(RuleType.LeaveAccrual, rule));
    }

    [Fact]
    public void A_non_numeric_value_on_a_numeric_field_is_rejected()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Conditions = [new RuleCondition { Field = "tenureMonths", Op = ">=", Value = "twelve" }],
            Action = new RuleAction { Type = "AccrueDays", Amount = 1.25m }
        };

        Assert.NotNull(RuleSchemaCatalog.Validate(RuleType.LeaveAccrual, rule));
    }

    [Fact]
    public void A_value_outside_an_enum_fields_options_is_rejected()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveApproval),
            Conditions = [new RuleCondition { Field = "leaveType", Op = "==", Value = "Sabbatical" }],
            Action = new RuleAction { Type = "AutoApprove" }
        };

        var problem = RuleSchemaCatalog.Validate(RuleType.LeaveApproval, rule);

        Assert.NotNull(problem);
        Assert.Contains("Sabbatical", problem);
    }

    [Fact]
    public void An_action_from_another_rule_type_is_rejected()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Action = new RuleAction { Type = "AutoApprove" }
        };

        Assert.NotNull(RuleSchemaCatalog.Validate(RuleType.LeaveAccrual, rule));
    }

    [Fact]
    public void A_missing_required_action_parameter_is_rejected()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Action = new RuleAction { Type = "AccrueDays" }
        };

        var problem = RuleSchemaCatalog.Validate(RuleType.LeaveAccrual, rule);

        Assert.NotNull(problem);
        Assert.Contains("Days per month", problem);
    }

    [Fact]
    public void An_optional_action_parameter_may_be_left_out()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Action = new RuleAction { Type = "AccrueDays", Amount = 1.25m } // no cap
        };

        Assert.Null(RuleSchemaCatalog.Validate(RuleType.LeaveAccrual, rule));
    }

    [Fact]
    public void A_rule_saved_under_the_wrong_type_is_rejected()
    {
        var rule = new RuleDefinition
        {
            RuleType = nameof(RuleType.OTMultiplier),
            Action = new RuleAction { Type = "AccrueDays", Amount = 1m }
        };

        Assert.NotNull(RuleSchemaCatalog.Validate(RuleType.LeaveAccrual, rule));
    }

    [Fact]
    public void ApprovalEscalation_is_marked_as_not_yet_wired()
    {
        // ApprovalService escalates from ApprovalChainStep.EscalateAfterDays rather than the engine.
        Assert.False(RuleSchemaCatalog.For(RuleType.ApprovalEscalation).Wired);
        Assert.All(RuleSchemaCatalog.All.Where(kv => kv.Key != RuleType.ApprovalEscalation),
            kv => Assert.True(kv.Value.Wired));
    }

    private static object SampleFor(RuleFieldSchema field) => field switch
    {
        { Kind: RuleValueKind.Enum, Options: { Length: > 0 } options } => options[0],
        { Kind: RuleValueKind.Text } => "sample",
        { Kind: RuleValueKind.Boolean } => false,
        _ => 99m
    };
}
