using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Core.RulesEngine.Schema;

/// <summary>Describes what a valid rule looks like for each rule type: the facts the evaluator will read.</summary>
public static class RuleSchemaCatalog
{
    private static readonly string[] NumericOps = { ">=", "<=", "==", "!=" };
    private static readonly string[] TextOps = { "==", "!=", "in" };
    private static readonly string[] EqualityOps = { "==", "!=" };

    /// <summary>"None" is what the attendance service sets when the day is an ordinary working day.</summary>
    private static readonly string[] HolidayTypeOptions =
        ["None", .. Enum.GetNames<HolidayType>()];

    private static readonly RuleFieldSchema TenureMonths = new(
        "tenureMonths", "Months of service", RuleValueKind.Integer, NumericOps, "months");

    // Shared by the rule types evaluated against a particular day.
    private static readonly RuleFieldSchema IsHoliday = new(
        "isHoliday", "Falls on a holiday", RuleValueKind.Boolean, EqualityOps);

    private static readonly RuleFieldSchema HolidayKind = new(
        "holidayType", "Holiday type", RuleValueKind.Enum, TextOps, Options: HolidayTypeOptions);

    private static RuleDefinition Starter(RuleType ruleType, string field, string op, object value, RuleAction action) =>
        new()
        {
            RuleType = ruleType.ToString(),
            Conditions = [new RuleCondition { Field = field, Op = op, Value = value }],
            Action = action
        };

    private static readonly IReadOnlyDictionary<RuleType, RuleTypeSchema> Schemas =
        new Dictionary<RuleType, RuleTypeSchema>
        {
            // Read by LeaveService.RunAccrualForEmployeeAsync
            [RuleType.LeaveAccrual] = new(
                nameof(RuleType.LeaveAccrual),
                "Leave accrual",
                "How much leave an employee earns, and the ceiling it stops at.",
                Fields:
                [
                    TenureMonths,
                    new("currentBalance", "Current leave balance", RuleValueKind.Decimal, NumericOps, "days"),
                ],
                Actions:
                [
                    new("AccrueDays", "Accrue leave days", "accrue {amount} days per month",
                    [
                        new("amount", "Days per month", RuleValueKind.Decimal, Required: true, "days"),
                        new("cap", "Maximum balance", RuleValueKind.Decimal, Required: false, "days"),
                    ]),
                ])
            {
                Starter = Starter(RuleType.LeaveAccrual, "tenureMonths", ">=", 0,
                    new RuleAction { Type = "AccrueDays", Amount = 1.25m, Cap = 15m })
            },

            // Read by LeaveService.RequestLeaveAsync
            [RuleType.LeaveApproval] = new(
                nameof(RuleType.LeaveApproval),
                "Leave approval",
                "Whether a leave request is approved automatically or routed to the approval chain.",
                Fields:
                [
                    TenureMonths,
                    new("days", "Days requested", RuleValueKind.Decimal, NumericOps, "days"),
                    new("leaveType", "Leave type", RuleValueKind.Enum, TextOps, Options: Enum.GetNames<LeaveType>()),
                ],
                Actions:
                [
                    new("AutoApprove", "Approve automatically", "approve the request without review", []),
                    new("RequireApproval", "Send for approval", "send the request through the approval chain", []),
                ])
            {
                Starter = Starter(RuleType.LeaveApproval, "days", "<=", 2,
                    new RuleAction { Type = "AutoApprove" })
            },

            // Read by AttendanceService.ComputeOtAsync
            [RuleType.OTMultiplier] = new(
                nameof(RuleType.OTMultiplier),
                "Overtime multiplier",
                "The rate overtime hours are paid at.",
                Fields:
                [
                    new("otHours", "Overtime hours", RuleValueKind.Decimal, NumericOps, "hours"),
                    IsHoliday,
                    HolidayKind,
                ],
                Actions:
                [
                    new("OTMultiplier", "Pay overtime at a multiplier", "pay overtime at {amount} times the normal rate",
                    [
                        new("amount", "Multiplier", RuleValueKind.Decimal, Required: true, null),
                    ]),
                ])
            {
                Starter = Starter(RuleType.OTMultiplier, "otHours", ">=", 0,
                    new RuleAction { Type = "OTMultiplier", Amount = 1.5m })
            },

            // Read by AttendanceService.ClockInAsync
            [RuleType.LateGracePeriod] = new(
                nameof(RuleType.LateGracePeriod),
                "Late grace period",
                "How long after the shift start an arrival is still not counted late.",
                Fields:
                [
                    new("minutesLate", "Minutes after shift start", RuleValueKind.Integer, NumericOps, "minutes"),
                    new("clockInHour", "Clock-in hour", RuleValueKind.Integer, NumericOps, "0 to 23"),
                    IsHoliday,
                    HolidayKind,
                ],
                Actions:
                [
                    new("LateGraceMinutes", "Allow a grace period", "allow {amount} minutes before counting the arrival late",
                    [
                        new("amount", "Grace period", RuleValueKind.Integer, Required: true, "minutes"),
                    ]),
                ])
            {
                Starter = Starter(RuleType.LateGracePeriod, "clockInHour", ">=", 9,
                    new RuleAction { Type = "LateGraceMinutes", Amount = 15m })
            },

            // No caller yet.
            [RuleType.ApprovalEscalation] = new(
                nameof(RuleType.ApprovalEscalation),
                "Approval escalation",
                "When a waiting approval step is raised to the next level.",
                Fields:
                [
                    new("daysWaiting", "Days waiting", RuleValueKind.Integer, NumericOps, "days"),
                ],
                Actions:
                [
                    new("EscalateAfterDays", "Escalate to the next level", "escalate after {amount} days",
                    [
                        new("amount", "Wait before escalating", RuleValueKind.Integer, Required: true, "days"),
                    ]),
                ])
            {
                Wired = false,
                Starter = Starter(RuleType.ApprovalEscalation, "daysWaiting", ">=", 3,
                    new RuleAction { Type = "EscalateAfterDays", Amount = 3m })
            },
        };

    public static IReadOnlyDictionary<RuleType, RuleTypeSchema> All => Schemas;

    public static RuleTypeSchema For(RuleType ruleType) => Schemas[ruleType];

    /// <summary>Checks a rule against the schema for its type.</summary>
    public static string? Validate(RuleType ruleType, RuleDefinition rule)
    {
        var schema = Schemas[ruleType];

        if (!string.IsNullOrWhiteSpace(rule.RuleType) &&
            !string.Equals(rule.RuleType, schema.RuleType, StringComparison.OrdinalIgnoreCase))
            return $"Rule says it is a '{rule.RuleType}' but it is being saved as '{schema.RuleType}'.";

        foreach (var condition in rule.Conditions)
        {
            var field = schema.Fields.FirstOrDefault(f =>
                string.Equals(f.Name, condition.Field, StringComparison.OrdinalIgnoreCase));

            if (field is null)
                return $"'{condition.Field}' is not a field of {schema.Label}. Available: {string.Join(", ", schema.Fields.Select(f => f.Name))}.";

            if (!field.Operators.Contains(condition.Op))
                return $"Operator '{condition.Op}' cannot be used with '{field.Name}'. Allowed: {string.Join(", ", field.Operators)}.";

            if (field.Kind is RuleValueKind.Integer or RuleValueKind.Decimal && !LooksNumeric(condition.Value))
                return $"'{field.Label}' expects a number but the rule has '{condition.Value}'.";

            if (field.Kind is RuleValueKind.Boolean && !bool.TryParse(condition.Value?.ToString(), out _))
                return $"'{field.Label}' expects true or false but the rule has '{condition.Value}'.";

            if (field is { Kind: RuleValueKind.Enum, Options: not null } && condition.Op != "in")
            {
                var value = condition.Value?.ToString();
                if (!field.Options.Any(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase)))
                    return $"'{value}' is not a valid {field.Label}. Allowed: {string.Join(", ", field.Options)}.";
            }
        }

        var action = schema.Actions.FirstOrDefault(a =>
            string.Equals(a.Type, rule.Action.Type, StringComparison.OrdinalIgnoreCase));

        if (action is null)
            return $"'{rule.Action.Type}' is not an action of {schema.Label}. Available: {string.Join(", ", schema.Actions.Select(a => a.Type))}.";

        foreach (var parameter in action.Parameters.Where(p => p.Required))
        {
            var supplied = parameter.Name switch
            {
                "amount" => rule.Action.Amount,
                "cap" => rule.Action.Cap,
                _ => null
            };
            if (supplied is null)
                return $"{action.Label} requires '{parameter.Label}'.";
        }

        return null;
    }

    private static bool LooksNumeric(object? value)
    {
        if (value is null) return false;
        if (value is int or long or decimal or double or float) return true;
        return decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _);
    }
}
