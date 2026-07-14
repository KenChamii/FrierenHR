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