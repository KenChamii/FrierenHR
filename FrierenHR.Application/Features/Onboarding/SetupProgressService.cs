using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;

namespace FrierenHR.Application.Features.Onboarding;

public interface ISetupProgressService
{
    /// <summary>What a new company still has to do, worked out from its own data rather than from a checklist somebody ticks off.</summary>
    Task<SetupProgressDto> GetAsync(Guid companyId, CancellationToken ct = default);
}

public class SetupProgressService : ISetupProgressService
{
    private readonly IDepartmentRepository _departments;
    private readonly IShiftRepository _shifts;
    private readonly IHolidayRepository _holidays;
    private readonly IEmployeeRepository _employees;
    private readonly IPayrollDeductionRepository _deductions;

    public SetupProgressService(
        IDepartmentRepository departments,
        IShiftRepository shifts,
        IHolidayRepository holidays,
        IEmployeeRepository employees,
        IPayrollDeductionRepository deductions)
    {
        _departments = departments;
        _shifts = shifts;
        _holidays = holidays;
        _employees = employees;
        _deductions = deductions;
    }

    public async Task<SetupProgressDto> GetAsync(Guid companyId, CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;

        var departments = await _departments.GetByCompanyAsync(companyId, ct);
        var shifts = await _shifts.GetByCompanyAsync(companyId, ct);
        var holidays = await _holidays.GetByCompanyAsync(companyId, ct);
        var employees = await _employees.GetByCompanyAsync(companyId, ct);
        var deductions = await _deductions.GetActiveAsync(companyId, ct);

        // Sign-up creates a default shift, so "has a shift" is always true and would start ticked.
        var shiftReviewed =
            shifts.Any(s => s.UpdatedAt is not null)
            || shifts.Count > 1;

        // More than one, because the administrator's own account was created by sign-up.
        var hasColleagues = employees.Count > 1;

        var steps = new List<SetupStepDto>
        {
            new("departments", "Add your departments",
                "The employee form reads them, and coverage reports group by them. Adding people first means going back to edit them.",
                "/departments", departments.Count > 0, false),

            new("shifts", "Check your working hours",
                "A shift sets the start time lateness is measured against, and its rest days decide which days leave skips. Sign-up guessed 9 to 6, resting weekends.",
                "/shifts", shiftReviewed, false),

            new("holidays", $"Add {year}'s holidays",
                "Leave counting, the payroll export and coverage all read the calendar. While it is empty, a request spanning a holiday still deducts that day.",
                "/holidays", holidays.Any(h => h.Date.Year == year), false),

            new("employees", "Add your first employee",
                "Set a monthly rate while you are there, or payroll will skip them when it runs.",
                "/employees", hasColleagues, false),

            new("deductions", "Add payroll deductions",
                "Only needed before you issue payslips. With none configured, net pay equals gross.",
                "/payroll-deductions", deductions.Count > 0, true),
        };

        var required = steps.Where(s => !s.Optional).ToList();
        var completed = required.Count(s => s.Done);

        return new SetupProgressDto(
            completed,
            required.Count,
            completed == required.Count,
            // Optional steps are never pointed at: they are offered, not pushed.
            steps.FirstOrDefault(s => !s.Done && !s.Optional)?.Route,
            steps);
    }
}
