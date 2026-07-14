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