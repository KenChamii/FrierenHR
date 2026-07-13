using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

public interface IRuleConfigRepository
{
    Task<List<CompanyRuleConfig>> GetActiveRulesAsync(Guid companyId, RuleType ruleType, CancellationToken ct = default);
    Task AddAsync(CompanyRuleConfig config, CancellationToken ct = default);
    void Update(CompanyRuleConfig config);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<CompanyRuleConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
}