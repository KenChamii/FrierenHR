using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class RuleConfigRepository : IRuleConfigRepository
{
    private readonly FrierenHRDbContext _context;
    public RuleConfigRepository(FrierenHRDbContext context) => _context = context;

    public async Task<List<CompanyRuleConfig>> GetActiveRulesAsync(Guid companyId, RuleType ruleType, CancellationToken ct = default) =>
        await _context.Set<CompanyRuleConfig>().AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.RuleType == ruleType && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task AddAsync(CompanyRuleConfig config, CancellationToken ct = default) =>
        await _context.Set<CompanyRuleConfig>().AddAsync(config, ct);

    public void Update(CompanyRuleConfig config) => _context.Set<CompanyRuleConfig>().Update(config);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);


    public async Task<CompanyRuleConfig?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
    await _context.Set<CompanyRuleConfig>().FirstOrDefaultAsync(r => r.Id == id, ct);
}