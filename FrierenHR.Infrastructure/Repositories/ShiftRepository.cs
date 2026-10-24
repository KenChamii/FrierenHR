using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class ShiftRepository : CompanyScopedRepository<Shift>, IShiftRepository
{
    public ShiftRepository(FrierenHRDbContext context) : base(context) { }

    protected override IOrderedQueryable<Shift> ApplyDefaultOrder(IQueryable<Shift> query) =>
        query.OrderByDescending(s => s.IsDefault).ThenBy(s => s.StartTime);

    public async Task<Shift?> GetDefaultAsync(Guid companyId, CancellationToken ct = default) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId && s.IsDefault, ct);

    /// <summary>One query with both shifts loaded, because resolving an employee's shift walks from them to their department and doing that lazily is three round trips per clock-in.</summary>
    public async Task<Employee?> GetEmployeeWithShiftsAsync(Guid employeeId, CancellationToken ct = default) =>
        await Context.Set<Employee>().AsNoTracking()
            .Include(e => e.Shift)
            .Include(e => e.Department)!.ThenInclude(d => d!.Shift)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
}
