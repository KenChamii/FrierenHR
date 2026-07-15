using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class ApprovalRepository : IApprovalRepository
{
    private readonly FrierenHRDbContext _context;
    public ApprovalRepository(FrierenHRDbContext context) => _context = context;

    public async Task<ApprovalChain> AddChainAsync(ApprovalChain chain, CancellationToken ct = default)
    {
        await _context.ApprovalChains.AddAsync(chain, ct);
        return chain;
    }

    public async Task<ApprovalChain?> GetChainByIdAsync(Guid chainId, CancellationToken ct = default) =>
        await _context.ApprovalChains.Include(c => c.Steps).FirstOrDefaultAsync(c => c.Id == chainId, ct);

    // one (e.g. different chains per leave type), this is the query to extend first.
    public async Task<ApprovalChain?> GetChainForCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await _context.ApprovalChains.Include(c => c.Steps).AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId, ct);

    public async Task<ApprovalInstance> AddInstanceAsync(ApprovalInstance instance, CancellationToken ct = default)
    {
        await _context.ApprovalInstances.AddAsync(instance, ct);
        return instance;
    }

    public async Task<ApprovalInstance?> GetInstanceByIdAsync(Guid instanceId, CancellationToken ct = default) =>
        await _context.ApprovalInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct);

    public void UpdateInstance(ApprovalInstance instance) => _context.ApprovalInstances.Update(instance);

    public async Task<List<ApprovalInstance>> GetAllPendingInstancesAsync(CancellationToken ct = default) =>
        await _context.ApprovalInstances.Where(i => i.Status == ApprovalStatus.Pending).ToListAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}