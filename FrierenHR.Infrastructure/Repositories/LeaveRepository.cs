using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class LeaveRepository : Repository<LeaveRequest>, ILeaveRepository
{
    public LeaveRepository(FrierenHRDbContext context) : base(context) { }

    public override async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.Include(l => l.Employee).Include(l => l.DecidedByEmployee)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<List<LeaveRequest>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        await DbSet.Include(l => l.Employee).AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.RequestedAt)
            .ToListAsync(ct);

    public async Task<List<LeaveRequest>> GetPendingForApproverAsync(Guid approverEmployeeId, CancellationToken ct = default) =>
        await DbSet.Include(l => l.Employee).AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Pending && l.Employee!.ManagerId == approverEmployeeId)
            .ToListAsync(ct);

    public async Task<LeaveBalance?> GetBalanceAsync(Guid employeeId, LeaveType leaveType, CancellationToken ct = default) =>
        await Context.Set<LeaveBalance>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.LeaveType == leaveType, ct);

    public async Task<List<LeaveBalance>> GetBalancesAsync(Guid employeeId, CancellationToken ct = default) =>
        await Context.Set<LeaveBalance>().AsNoTracking()
            .Where(b => b.EmployeeId == employeeId)
            .ToListAsync(ct);

    public async Task<List<LeaveRequest>> GetApprovedInRangeAsync(
        Guid companyId, DateTime from, DateTime to, Guid? departmentId = null, CancellationToken ct = default)
    {
        var query = DbSet.Include(l => l.Employee).AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Approved
                        && l.Employee!.CompanyId == companyId
                        // Overlap, not containment: leave that starts before the window and
                        // ends after it still covers every day in between.
                        && l.StartDate <= to
                        && l.EndDate >= from);

        if (departmentId is not null)
            query = query.Where(l => l.Employee!.DepartmentId == departmentId);

        return await query.OrderBy(l => l.StartDate).ToListAsync(ct);
    }

    public async Task<List<LeaveBalance>> GetBalancesForCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await Context.Set<LeaveBalance>().AsNoTracking()
            .Include(b => b.Employee)
            .Where(b => b.Employee!.CompanyId == companyId && b.Employee.IsActive)
            .ToListAsync(ct);

    public async Task UpsertBalanceAsync(LeaveBalance balance, CancellationToken ct = default)
    {
        var existing = await Context.Set<LeaveBalance>()
            .FirstOrDefaultAsync(b => b.EmployeeId == balance.EmployeeId && b.LeaveType == balance.LeaveType, ct);

        if (existing is null)
        {
            await Context.Set<LeaveBalance>().AddAsync(balance, ct);
        }
        else
        {
            existing.Balance = balance.Balance;
            existing.LastAccrualDate = balance.LastAccrualDate;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        // Intentionally no SaveChangesAsync here — caller batches it (see notes above).
    }
}