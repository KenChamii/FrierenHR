using FrierenHR.Core.Common;


namespace FrierenHR.Core.Entities;

public class Department : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}