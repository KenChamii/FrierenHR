using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class HolidayRepository : CompanyScopedRepository<Holiday>, IHolidayRepository
{
    public HolidayRepository(FrierenHRDbContext context) : base(context) { }

    protected override IOrderedQueryable<Holiday> ApplyDefaultOrder(IQueryable<Holiday> query) =>
        query.OrderBy(h => h.Date);

    public async Task<List<Holiday>> GetForRangeAsync(
        Guid companyId, DateOnly from, DateOnly to, Guid? departmentId = null, CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            // A department-specific holiday applies on top of the company-wide ones, so this is "company-wide OR mine".
            .Where(h => h.CompanyId == companyId
                        && h.Date >= from && h.Date <= to
                        && (h.DepartmentId == null || h.DepartmentId == departmentId))
            .OrderBy(h => h.Date)
            .ToListAsync(ct);

    public async Task<Holiday?> GetByDateAsync(
        Guid companyId, DateOnly date, Guid? departmentId, CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(h => h.CompanyId == companyId && h.Date == date && h.DepartmentId == departmentId, ct);
}
