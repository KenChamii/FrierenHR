using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

/// <summary>
/// Read-only by design. Audit rows are written by the save interceptor, and nothing in the
/// application is allowed to amend or delete them, so no write methods are offered here.
/// </summary>
public class AuditRepository : IAuditRepository
{
    private readonly FrierenHRDbContext _context;
    public AuditRepository(FrierenHRDbContext context) => _context = context;

    public async Task<List<AuditEntry>> GetForCompanyAsync(
        Guid companyId, string? entityName = null, Guid? entityId = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        await Filter(companyId, entityName, entityId)
            .OrderByDescending(a => a.ChangedAt)
            .Skip(skip)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(ct);

    public Task<int> CountForCompanyAsync(
        Guid companyId, string? entityName = null, Guid? entityId = null, CancellationToken ct = default) =>
        Filter(companyId, entityName, entityId).CountAsync(ct);

    private IQueryable<AuditEntry> Filter(Guid companyId, string? entityName, Guid? entityId)
    {
        var query = _context.Set<AuditEntry>().AsNoTracking().Where(a => a.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);
        if (entityId is not null) query = query.Where(a => a.EntityId == entityId);
        return query;
    }
}
