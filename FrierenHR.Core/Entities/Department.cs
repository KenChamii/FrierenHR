using FrierenHR.Core.Common;


namespace FrierenHR.Core.Entities;

public class Department : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Applies to everyone in the department who has no shift of their own.</summary>
    public Guid? ShiftId { get; set; }
    public Shift? Shift { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
