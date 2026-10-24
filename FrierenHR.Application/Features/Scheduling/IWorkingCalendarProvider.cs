using FrierenHR.Core.Time;

namespace FrierenHR.Application.Features.Scheduling;

/// <summary>Builds a <see cref="WorkingCalendar"/> for a date range by combining the company's holidays with the rest days of the applicable shift.</summary>
public interface IWorkingCalendarProvider
{
    /// <summary>Holidays for the employee's company and department, with their shift's rest days.</summary>
    Task<WorkingCalendar> ForEmployeeAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Company-wide view, for reports that are not about one person.</summary>
    Task<WorkingCalendar> ForCompanyAsync(
        Guid companyId, DateOnly from, DateOnly to, Guid? departmentId = null, CancellationToken ct = default);
}
