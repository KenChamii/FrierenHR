using FrierenHR.Core.RulesEngine;
using FrierenHR.Core.RulesEngine.Evaluators;
using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Tests.RulesEngine;

public class ActionExecutorTests
{
    [Fact]
    public void AccrueDays_returns_the_configured_amount_when_there_is_room_under_the_cap()
    {
        var action = new RuleAction { Type = "AccrueDays", Amount = 1.25m, Cap = 15m };

        var result = ActionExecutor.Execute(action, new RuleContext().Set("currentBalance", 8m));

        Assert.True(result.Matched);
        Assert.True(result.Success);
        Assert.Equal(1.25m, result.ResultValue);
    }

    [Fact]
    public void AccrueDays_is_trimmed_so_the_balance_lands_exactly_on_the_cap()
    {
        var action = new RuleAction { Type = "AccrueDays", Amount = 1.25m, Cap = 15m };

        var result = ActionExecutor.Execute(action, new RuleContext().Set("currentBalance", 14.5m));

        Assert.Equal(0.5m, result.ResultValue);
    }

    [Fact]
    public void AccrueDays_returns_zero_once_the_balance_is_at_or_over_the_cap()
    {
        var action = new RuleAction { Type = "AccrueDays", Amount = 1.25m, Cap = 15m };

        Assert.Equal(0m, ActionExecutor.Execute(action, new RuleContext().Set("currentBalance", 15m)).ResultValue);
        Assert.Equal(0m, ActionExecutor.Execute(action, new RuleContext().Set("currentBalance", 20m)).ResultValue);
    }

    [Fact]
    public void AccrueDays_ignores_the_cap_when_the_balance_is_not_in_the_context()
    {
        var action = new RuleAction { Type = "AccrueDays", Amount = 1.25m, Cap = 15m };

        var result = ActionExecutor.Execute(action, new RuleContext());

        Assert.Equal(1.25m, result.ResultValue);
    }

    [Fact]
    public void AccrueDays_without_an_amount_fails()
    {
        var result = ActionExecutor.Execute(new RuleAction { Type = "AccrueDays" }, new RuleContext());

        Assert.False(result.Success);
        Assert.False(result.Matched);
        Assert.Contains("amount", result.Message);
    }

    [Fact]
    public void OTMultiplier_multiplies_the_overtime_hours()
    {
        var action = new RuleAction { Type = "OTMultiplier", Amount = 1.5m };

        var result = ActionExecutor.Execute(action, new RuleContext().Set("otHours", 4m));

        Assert.Equal(6m, result.ResultValue);
        Assert.Equal("OTMultiplier", result.ActionType);
    }

    [Fact]
    public void OTMultiplier_yields_zero_when_no_overtime_was_worked()
    {
        var action = new RuleAction { Type = "OTMultiplier", Amount = 1.5m };

        Assert.Equal(0m, ActionExecutor.Execute(action, new RuleContext()).ResultValue);
    }

    [Fact]
    public void OTMultiplier_without_an_amount_fails()
    {
        var result = ActionExecutor.Execute(new RuleAction { Type = "OTMultiplier" }, new RuleContext());

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("AutoApprove")]
    [InlineData("RequireApproval")]
    public void Approval_actions_match_and_report_their_type(string type)
    {
        var result = ActionExecutor.Execute(new RuleAction { Type = type }, new RuleContext());

        Assert.True(result.Matched);
        Assert.True(result.Success);
        Assert.Equal(type, result.ActionType);
    }

    [Fact]
    public void LateGraceMinutes_returns_the_grace_period()
    {
        var result = ActionExecutor.Execute(new RuleAction { Type = "LateGraceMinutes", Amount = 15m }, new RuleContext());

        Assert.Equal(15m, result.ResultValue);
    }

    [Fact]
    public void EscalateAfterDays_returns_the_wait()
    {
        var result = ActionExecutor.Execute(new RuleAction { Type = "EscalateAfterDays", Amount = 3m }, new RuleContext());

        Assert.Equal(3m, result.ResultValue);
    }

    [Fact]
    public void An_unknown_action_type_fails_rather_than_silently_doing_nothing()
    {
        var result = ActionExecutor.Execute(new RuleAction { Type = "GrantSabbatical" }, new RuleContext());

        Assert.False(result.Success);
        Assert.Contains("GrantSabbatical", result.Message);
    }
}
