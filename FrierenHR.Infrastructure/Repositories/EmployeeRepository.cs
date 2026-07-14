using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(FrierenHRDbContext context) : base(context) { }

    public override async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.Include(e => e.Department).Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<List<Employee>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await DbSet.Include(e => e.Department).Include(e => e.Manager).AsNoTracking()
            .Where(e => e.CompanyId == companyId)
            .ToListAsync(ct);

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await DbSet.Include(e => e.Department).Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Email == email, ct);
    // Not .AsNoTracking() — AuthController's login flow reads through this method,
    // and while it doesn't mutate the entity today, GetByEmailAsync is also the
    // natural place a future "update last login timestamp" would hang off of.

    public async Task<List<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct = default) =>
        await DbSet.Include(e => e.Department).AsNoTracking()
            .Where(e => e.ManagerId == managerId)
            .ToListAsync(ct);
}