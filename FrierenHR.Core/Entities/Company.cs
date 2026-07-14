using FrierenHR.Core.Common;

namespace FrierenHR.Core.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // short unique code e.g. "ACME"
    public bool IsActive { get; set; } = true;
    public ICollection<CompanyRuleConfig> RuleConfigs { get; set; } = new List<CompanyRuleConfig>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}