using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

public class ApprovalChain : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Standard Leave Approval"
    public ICollection<ApprovalChainStep> Steps { get; set; } = new List<ApprovalChainStep>();
}

public class ApprovalChainStep : BaseEntity
{
    public Guid ApprovalChainId { get; set; }
    public ApprovalChain? ApprovalChain { get; set; }
    public int StepOrder { get; set; }
    public EmployeeRole ApproverRole { get; set; }
    public int EscalateAfterDays { get; set; } = 3;
}

// A running instance against one specific leave request
public class ApprovalInstance : BaseEntity
{
    public Guid LeaveRequestId { get; set; }
    public Guid ApprovalChainId { get; set; }
    public ApprovalChain? ApprovalChain { get; set; }
    public int CurrentStepOrder { get; set; } = 1;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime StepStartedAt { get; set; } = DateTime.UtcNow;
}