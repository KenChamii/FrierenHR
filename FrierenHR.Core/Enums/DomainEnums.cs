namespace FrierenHR.Core.Enums;

public enum EmployeeRole { Employee = 0, Manager = 1, HRAdmin = 2 }
public enum LeaveStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3, Escalated = 4 }
public enum LeaveType { Vacation = 0, Sick = 1, Emergency = 2, Unpaid = 3, Maternity = 4, Paternity = 5 }
public enum RuleType { LeaveAccrual = 0, LeaveApproval = 1, OTMultiplier = 2, LateGracePeriod = 3, ApprovalEscalation = 4 }
public enum ConversationType { Direct = 0, Group = 1, Broadcast = 2 }
public enum ApprovalStatus { Pending = 0, Approved = 1, Rejected = 2, Escalated = 3 }