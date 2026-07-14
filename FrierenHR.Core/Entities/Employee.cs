using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;


namespace FrierenHR.Core.Entities;

public class Employee : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public EmployeeRole Role { get; set; } = EmployeeRole.Employee;
    public bool IsActive { get; set; } = true;
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();

    public int TenureMonths(DateTime? asOf = null)
    {
        var reference = asOf ?? DateTime.UtcNow;
        int months = (reference.Year - HireDate.Year) * 12 + (reference.Month - HireDate.Month);
        if (reference.Day < HireDate.Day) months--;
        return Math.Max(0, months);
    }
}