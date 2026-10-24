using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class CompanyRepository : Repository<Company>, ICompanyRepository
{
    public CompanyRepository(FrierenHRDbContext context) : base(context) { }

    // Companies has few enough rows that eager-loading Employees here (for the EmployeeCount used by CompanyService's DTO mapping) is fine.
    public override async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.Include(c => c.Employees).FirstOrDefaultAsync(c => c.Id == id, ct);

    public override async Task<List<Company>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.Include(c => c.Employees).AsNoTracking().ToListAsync(ct);

    public async Task<Company?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code, ct);
}