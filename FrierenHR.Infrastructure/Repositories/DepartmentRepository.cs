using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(FrierenHRDbContext context) : base(context) { }

    public async Task<List<Department>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await DbSet.Include(d => d.Employees).AsNoTracking()
            .Where(d => d.CompanyId == companyId)
            .ToListAsync(ct);
}