namespace FrierenHR.Core.RulesEngine.Models;

// Fixed action-type catalog. Extend the switch in ActionExecutor + this comment together when adding a type:
//   AccrueDays (Leave) | RequireApproval / AutoApprove (Leave/Approval) | OTMultiplier / LateGraceMinutes (Attendance) | EscalateAfterDays (Approval)
public class RuleAction
{
    public string Type { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public decimal? Cap { get; set; }
    public Dictionary<string, object>? ExtraParams { get; set; }
}