using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

public interface IRuleConfigRepository
{
    Task<List<CompanyRuleConfig>> GetActiveRulesAsync(Guid companyId, RuleType ruleType, CancellationToken ct = default);
    Task AddAsync(CompanyRuleConfig config, CancellationToken ct = default);
    void Update(CompanyRuleConfig config);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<CompanyRuleConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
public interface ICompanyRepository : IRepository<Company> { Task<Company?> GetByCodeAsync(string code, CancellationToken ct = default); }
public interface IDepartmentRepository : IRepository<Department> { Task<List<Department>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default); }
public interface IEmployeeRepository : IRepository<Employee>
{
    Task<List<Employee>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct = default);
}

public interface ILeaveRepository : IRepository<LeaveRequest>
{
    Task<List<LeaveRequest>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<List<LeaveRequest>> GetPendingForApproverAsync(Guid approverEmployeeId, CancellationToken ct = default);
    Task<LeaveBalance?> GetBalanceAsync(Guid employeeId, LeaveType leaveType, CancellationToken ct = default);
    Task<List<LeaveBalance>> GetBalancesAsync(Guid employeeId, CancellationToken ct = default);
    Task UpsertBalanceAsync(LeaveBalance balance, CancellationToken ct = default);
}

public interface IAttendanceRepository : IRepository<AttendanceLog>
{
    Task<AttendanceLog?> GetOpenLogAsync(Guid employeeId, CancellationToken ct = default);
    Task<List<AttendanceLog>> GetByEmployeeAsync(Guid employeeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}

public interface ITimesheetRepository : IRepository<Timesheet>
{
    Task<Timesheet?> GetForWeekAsync(Guid employeeId, DateTime weekStartDate, CancellationToken ct = default);
    Task<List<Timesheet>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<List<Timesheet>> GetPendingForManagerAsync(Guid managerId, CancellationToken ct = default);
    Task<List<Timesheet>> GetAllPendingAsync(CancellationToken ct = default);
}

public interface IApprovalRepository
{
    Task<ApprovalChain> AddChainAsync(ApprovalChain chain, CancellationToken ct = default);
    Task<ApprovalChain?> GetChainByIdAsync(Guid chainId, CancellationToken ct = default);
    Task<ApprovalChain?> GetChainForCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<ApprovalInstance> AddInstanceAsync(ApprovalInstance instance, CancellationToken ct = default);
    Task<ApprovalInstance?> GetInstanceByIdAsync(Guid instanceId, CancellationToken ct = default);
    void UpdateInstance(ApprovalInstance instance);
    Task<List<ApprovalInstance>> GetAllPendingInstancesAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}


public interface IMessagingRepository
{
    Task<Conversation?> GetDirectConversationAsync(Guid employeeAId, Guid employeeBId, CancellationToken ct = default);
    Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken ct = default);
    Task<List<ConversationDto>> GetConversationsForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<Message> AddMessageAsync(Message message, CancellationToken ct = default);
    Task<List<MessageDto>> GetHistoryAsync(Guid conversationId, int skip, int take, CancellationToken ct = default);
}