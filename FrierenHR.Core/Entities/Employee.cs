using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;


namespace FrierenHR.Core.Entities;

public class Employee : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    /// <summary>Overrides the department's shift. Null falls back to the department, then the company default.</summary>
    public Guid? ShiftId { get; set; }
    public Shift? Shift { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// True while the account still carries a password somebody else chose. Set when HR
    /// creates an account or issues a reset, cleared when the person sets their own.
    ///
    /// Until it is cleared the application lets them change their password and nothing else,
    /// so an administrator's knowledge of the temporary password is short-lived.
    /// </summary>
    public bool MustChangePassword { get; set; }
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Gross monthly pay before deductions. Null for an employee whose pay has not been
    /// recorded, which is why payslip generation skips them rather than pricing them at zero.
    /// </summary>
    public decimal? MonthlyRate { get; set; }

    /// <summary>
    /// Hours a full month is considered to be, used to derive the hourly rate. Held per
    /// employee rather than per company so a part-timer on the same monthly cycle can be
    /// priced correctly. 174 is 8 hours across 261 working days, divided by 12.
    /// </summary>
    public decimal StandardMonthlyHours { get; set; } = 174m;

    /// <summary>Hourly rate implied by the monthly pay, or null when no rate is recorded.</summary>
    public decimal? HourlyRate() =>
        MonthlyRate is { } monthly && StandardMonthlyHours > 0
            ? monthly / StandardMonthlyHours
            : null;

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