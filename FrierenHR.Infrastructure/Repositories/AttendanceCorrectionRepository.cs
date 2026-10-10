using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class AttendanceCorrectionRepository : Repository<AttendanceCorrection>, IAttendanceCorrectionRepository
{
    public AttendanceCorrectionRepository(FrierenHRDbContext context) : base(context) { }

    public async Task<List<AttendanceCorrection>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.Date)
            .ToListAsync(ct);

    public async Task<List<AttendanceCorrection>> GetPendingForApproverAsync(
        Guid approverEmployeeId, bool companyWide, CancellationToken ct = default)
    {
        var query = DbSet.AsNoTracking()
            .Include(c => c.Employee)
            .Where(c => c.Status == ApprovalStatus.Pending);

        // HR sees the whole company; a manager sees only their own reports. Matches how
        // TimesheetService.GetPendingForApproverAsync already splits the two.
        if (!companyWide)
            query = query.Where(c => c.Employee != null && c.Employee.ManagerId == approverEmployeeId);
        else
            query = query.Where(c => c.Employee != null
                && Context.Set<Employee>().Any(a => a.Id == approverEmployeeId && a.CompanyId == c.Employee.CompanyId));

        return await query.OrderBy(c => c.Date).ToListAsync(ct);
    }

    public async Task<AttendanceCorrection?> GetPendingForDateAsync(
        Guid employeeId, DateOnly date, CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmployeeId == employeeId && c.Date == date && c.Status == ApprovalStatus.Pending, ct);
}
