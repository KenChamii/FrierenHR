using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using FrierenHR.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

public class AttendanceRepository : Repository<AttendanceLog>, IAttendanceRepository
{
    public AttendanceRepository(FrierenHRDbContext context) : base(context) { }

    public async Task<AttendanceLog?> GetOpenLogAsync(Guid employeeId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.TimeOut == null, ct);

    public async Task<List<AttendanceLog>> GetByEmployeeAsync(Guid employeeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = DbSet.AsNoTracking().Where(a => a.EmployeeId == employeeId);
        if (from is not null) query = query.Where(a => a.TimeIn >= from);
        if (to is not null) query = query.Where(a => a.TimeIn <= to);
        return await query.OrderByDescending(a => a.TimeIn).ToListAsync(ct);
    }
}