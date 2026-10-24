using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

/// <summary>A named deduction a company applies to every payslip, such as a provident fund contribution or a staff loan repayment.</summary>
public class PayrollDeduction : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;
    public DeductionKind Kind { get; set; }

    /// <summary>An amount when the kind is FixedAmount, a percentage when PercentOfGross.</summary>
    public decimal Value { get; set; }

    /// <summary>Order on the payslip, so the lines read the same way every period.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>A payslip as issued.</summary>
public class Payslip : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    /// <summary>The rate used, kept so an old payslip still explains itself after a raise.</summary>
    public decimal HourlyRate { get; set; }
    public decimal MonthlyRate { get; set; }

    public decimal RegularHours { get; set; }

    /// <summary>Overtime hours after the company's multiplier, as the payroll export reports them.</summary>
    public decimal PayableOvertimeHours { get; set; }

    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PayslipLine> Lines { get; set; } = new List<PayslipLine>();
}

/// <summary>One line of a payslip.</summary>
public class PayslipLine : BaseEntity
{
    public Guid PayslipId { get; set; }
    public Payslip? Payslip { get; set; }

    public string Label { get; set; } = string.Empty;
    public PayslipLineKind Kind { get; set; }

    /// <summary>Always positive. The kind decides whether it adds or subtracts.</summary>
    public decimal Amount { get; set; }

    /// <summary>How the amount was arrived at, for example "12.5 h x 250.00".</summary>
    public string? Basis { get; set; }

    public int SortOrder { get; set; }
}
