using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Enums;
using FrierenHR.Core.Time;

namespace FrierenHR.Application.Features.Scheduling;

public class WorkingCalendarProvider : IWorkingCalendarProvider
{
    /// <summary>
    /// Used when a company has defined no shifts. Saturday and Sunday is the common case and
    /// is a better default than treating every day as a working day, which would deduct
    /// weekend days from someone's leave balance.
    /// </summary>
    private const WeekDays FallbackRestDays = WeekDays.Weekend;

    private readonly IHolidayRepository _holidayRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftService _shiftService;

    public WorkingCalendarProvider(
        IHolidayRepository holidayRepository, IShiftRepository shiftRepository, IShiftService shiftService)
    {
        _holidayRepository = holidayRepository;
        _shiftRepository = shiftRepository;
        _shiftService = shiftService;
    }

    public async Task<WorkingCalendar> ForEmployeeAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var employee = await _shiftRepository.GetEmployeeWithShiftsAsync(employeeId, ct);
        if (employee is null) return WorkingCalendar.RestDaysOnly(FallbackRestDays);

        var shift = await _shiftService.ResolveForEmployeeAsync(employeeId, ct);
        var holidays = await _holidayRepository.GetForRangeAsync(
            employee.CompanyId, from, to, employee.DepartmentId, ct);

        return new WorkingCalendar(
            holidays.Select(h => (h.Date, h.Type)),
            shift?.RestDays ?? FallbackRestDays);
    }

    public async Task<WorkingCalendar> ForCompanyAsync(
        Guid companyId, DateOnly from, DateOnly to, Guid? departmentId = null, CancellationToken ct = default)
    {
        var holidays = await _holidayRepository.GetForRangeAsync(companyId, from, to, departmentId, ct);
        var defaultShift = await _shiftRepository.GetDefaultAsync(companyId, ct);

        return new WorkingCalendar(
            holidays.Select(h => (h.Date, h.Type)),
            defaultShift?.RestDays ?? FallbackRestDays);
    }
}
