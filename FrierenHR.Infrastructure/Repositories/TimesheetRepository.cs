using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Infrastructure.Data;
using FrierenHR.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

public class TimesheetRepository : Repository<Timesheet>, ITimesheetRepository
{
    public TimesheetRepository(FrierenHRDbContext context) : base(context) { }

    public async Task<Timesheet?> GetForWeekAsync(Guid employeeId, DateTime weekStartDate, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStartDate, ct);

    public async Task<List<Timesheet>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        await DbSet.AsNoTracking().Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.WeekStartDate).ToListAsync(ct);

    public async Task<List<Timesheet>> GetPendingForManagerAsync(Guid managerId, CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .Where(t => t.Status == TimesheetStatus.Submitted && t.Employee!.ManagerId == managerId)
            .OrderBy(t => t.SubmittedAt).ToListAsync(ct);

    public async Task<List<Timesheet>> GetAllPendingAsync(CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .Where(t => t.Status == TimesheetStatus.Submitted)
            .OrderBy(t => t.SubmittedAt).ToListAsync(ct);
}
