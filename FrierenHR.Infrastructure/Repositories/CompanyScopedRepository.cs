using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Common;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

/// <summary>Base for repositories over company-scoped entities.</summary>
public abstract class CompanyScopedRepository<T> : Repository<T>, ICompanyScopedRepository<T>
    where T : BaseEntity, ICompanyScoped
{
    protected CompanyScopedRepository(FrierenHRDbContext context) : base(context) { }

    /// <summary>Ordering differs per entity, so each repository says what "in order" means.</summary>
    protected abstract IOrderedQueryable<T> ApplyDefaultOrder(IQueryable<T> query);

    public virtual async Task<List<T>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await ApplyDefaultOrder(DbSet.AsNoTracking().Where(x => x.CompanyId == companyId)).ToListAsync(ct);

    /// <summary>Tracked, because callers load a row in order to change it.</summary>
    public virtual async Task<T?> GetForCompanyAsync(Guid id, Guid companyId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
}
