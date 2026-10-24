using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Features.Scheduling;
// FrierenHR.Application.Features.Employee is a namespace.
using EmployeeEntity = FrierenHR.Core.Entities.Employee;

namespace FrierenHR.Application.Features.Reporting;

/// <summary>Who is in and who is out, day by day.</summary>
public class AvailabilityService : IAvailabilityService
{
    /// <summary>Days called out as thin, most exposed first.</summary>
    private const int ThinDaysReported = 3;

    private const string NoDepartment = "Unassigned";

    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly ILeaveRepository _leave;
    private readonly IWorkingCalendarProvider _calendars;

    public AvailabilityService(
        IEmployeeRepository employees,
        IDepartmentRepository departments,
        ILeaveRepository leave,
        IWorkingCalendarProvider calendars)
    {
        _employees = employees;
        _departments = departments;
        _leave = leave;
        _calendars = calendars;
    }

    public async Task<AvailabilityDto> GetAsync(
        Guid companyId, DateOnly from, DateOnly to, Guid? departmentId = null, CancellationToken ct = default)
    {
        if (to < from) throw new InvalidOperationException("The end date cannot be before the start date.");

        var employees = (await _employees.GetByCompanyAsync(companyId, ct))
            .Where(e => e.IsActive)
            .Where(e => departmentId is null || e.DepartmentId == departmentId)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToList();

        if (employees.Count == 0)
            return new AvailabilityDto(from, to, [], []);

        var departmentNames = (await _departments.GetByCompanyAsync(companyId, ct))
            .ToDictionary(d => d.Id, d => d.Name);

        // One query for everyone's leave, then grouped in memory.
        var leaveByEmployee = (await _leave.GetApprovedInRangeAsync(
                companyId, from.ToDateTime(TimeOnly.MinValue), to.ToDateTime(TimeOnly.MaxValue), departmentId, ct))
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // The calendar is per employee because rest days follow their shift.
        var rows = new List<AvailabilityRowDto>(employees.Count);
        foreach (var employee in employees)
        {
            var calendar = await _calendars.ForEmployeeAsync(employee.Id, from, to, ct);
            var leave = leaveByEmployee.GetValueOrDefault(employee.Id) ?? [];

            var days = new List<AvailabilityDayDto>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var onLeave = leave.FirstOrDefault(l =>
                    DateOnly.FromDateTime(l.StartDate) <= date && DateOnly.FromDateTime(l.EndDate) >= date);

                // Leave first: someone on leave is out, whatever else the day is.
                days.Add(onLeave is not null
                    ? new AvailabilityDayDto(date, DayAvailability.OnLeave, onLeave.LeaveType.ToString())
                    : calendar.IsHoliday(date)
                        ? new AvailabilityDayDto(date, DayAvailability.Holiday, calendar.HolidayTypeOn(date)?.ToString())
                        : calendar.IsRestDay(date)
                            ? new AvailabilityDayDto(date, DayAvailability.RestDay, null)
                            : new AvailabilityDayDto(date, DayAvailability.Working, null));
            }

            rows.Add(new AvailabilityRowDto(
                employee.Id,
                $"{employee.FirstName} {employee.LastName}",
                DepartmentNameOf(employee, departmentNames),
                days));
        }

        return new AvailabilityDto(from, to, rows, ThinnestDays(rows, employees, departmentNames));
    }

    /// <summary>Coverage per department per day, worst first.</summary>
    private static List<CoverageDto> ThinnestDays(
        List<AvailabilityRowDto> rows,
        List<EmployeeEntity> employees,
        IReadOnlyDictionary<Guid, string> departmentNames)
    {
        var departmentOf = employees.ToDictionary(e => e.Id, e => DepartmentNameOf(e, departmentNames));

        var coverage = rows
            .SelectMany(row => row.Days.Select(day => new
            {
                day.Date,
                Department = departmentOf.GetValueOrDefault(row.EmployeeId, NoDepartment),
                day.Status
            }))
            // A rest day or holiday is not a gap in cover, it is a closed day.
            .Where(x => x.Status is DayAvailability.Working or DayAvailability.OnLeave)
            .GroupBy(x => new { x.Date, x.Department })
            .Select(g =>
            {
                var total = g.Count();
                var working = g.Count(x => x.Status == DayAvailability.Working);
                return new CoverageDto(
                    g.Key.Date, g.Key.Department, working, total,
                    total == 0 ? 100 : (int)Math.Round(working * 100d / total));
            })
            .Where(c => c.PercentWorking < 100)
            .OrderBy(c => c.PercentWorking)
            .ThenBy(c => c.Date)
            .Take(ThinDaysReported)
            .ToList();

        return coverage;
    }

    private static string DepartmentNameOf(
        EmployeeEntity employee, IReadOnlyDictionary<Guid, string> names) =>
        employee.DepartmentId is not null && names.TryGetValue(employee.DepartmentId.Value, out var name)
            ? name
            : NoDepartment;
}
