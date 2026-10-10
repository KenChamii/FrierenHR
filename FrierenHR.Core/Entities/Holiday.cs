using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

/// <summary>
/// A non-working day observed by one company. Per company rather than global, because
/// branches in different regions observe different local holidays.
/// </summary>
public class Holiday : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>DateOnly, not DateTime: a holiday has no time of day and no time zone.</summary>
    public DateOnly Date { get; set; }

    public string Name { get; set; } = string.Empty;
    public HolidayType Type { get; set; } = HolidayType.Regular;

    /// <summary>
    /// Set when the holiday applies to one department rather than the whole company, for a
    /// site-specific founding day or local fiesta. Null means company-wide.
    /// </summary>
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
