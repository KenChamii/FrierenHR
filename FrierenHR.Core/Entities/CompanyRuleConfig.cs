using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

public class CompanyRuleConfig : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public RuleType RuleType { get; set; }
    public string RuleJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0; // lets a company stack tiered rules of the same type
}