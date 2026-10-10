using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;

namespace FrierenHR.Application.Features.Scheduling;

public class ShiftService : IShiftService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public ShiftService(IShiftRepository shiftRepository, IEmployeeRepository employeeRepository)
    {
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<List<ShiftDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default)
    {
        var shifts = await _shiftRepository.GetByCompanyAsync(companyId, ct);
        var employees = await _employeeRepository.GetByCompanyAsync(companyId, ct);

        // Counted here rather than in SQL because the list is per company and already loaded,
        // and a count per row would be one query each.
        return shifts
            .Select(s => ToDto(s, employees.Count(e => e.ShiftId == s.Id)))
            .ToList();
    }

    public async Task<ShiftDto> SaveAsync(SaveShiftDto dto, CancellationToken ct = default)
    {
        Validate(dto);

        var entity = dto.Id is null
            ? new Shift { CompanyId = dto.CompanyId }
            : await _shiftRepository.GetForCompanyAsync(dto.Id.Value, dto.CompanyId, ct)
              ?? throw new InvalidOperationException($"Shift '{dto.Id}' not found for this company.");

        // At most one default per company, enforced by a filtered unique index. Clearing the
        // previous one here turns a constraint violation into the behaviour people expect.
        if (dto.IsDefault)
        {
            var existingDefault = await _shiftRepository.GetDefaultAsync(dto.CompanyId, ct);
            if (existingDefault is not null && existingDefault.Id != entity.Id)
            {
                var tracked = await _shiftRepository.GetForCompanyAsync(existingDefault.Id, dto.CompanyId, ct);
                if (tracked is not null)
                {
                    tracked.IsDefault = false;
                    tracked.UpdatedAt = DateTime.UtcNow;
                    _shiftRepository.Update(tracked);
                }
            }
        }

        entity.Name = dto.Name.Trim();
        entity.StartTime = dto.StartTime;
        entity.EndTime = dto.EndTime;
        entity.BreakMinutes = dto.BreakMinutes;
        entity.RestDays = dto.RestDays;
        entity.IsDefault = dto.IsDefault;

        if (dto.Id is null) await _shiftRepository.AddAsync(entity, ct);
        else { entity.UpdatedAt = DateTime.UtcNow; _shiftRepository.Update(entity); }

        await _shiftRepository.SaveChangesAsync(ct);
        return ToDto(entity, 0);
    }

    public async Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct = default)
    {
        var shift = await _shiftRepository.GetForCompanyAsync(id, companyId, ct)
            ?? throw new InvalidOperationException($"Shift '{id}' not found for this company.");

        // The foreign keys are Restrict, so this would fail at the database with an opaque
        // message. Say what to do instead.
        var employees = await _employeeRepository.GetByCompanyAsync(companyId, ct);
        var assigned = employees.Count(e => e.ShiftId == id);
        if (assigned > 0)
            throw new InvalidOperationException(
                $"{assigned} employee(s) are on this shift. Move them to another shift before deleting it.");

        _shiftRepository.Remove(shift);
        await _shiftRepository.SaveChangesAsync(ct);
    }

    public async Task<Shift?> ResolveForEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        (await ResolveAsync(employeeId, ct)).Shift;

    public async Task<ResolvedShiftDto> DescribeForEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        var (shift, source) = await ResolveAsync(employeeId, ct);
        return new ResolvedShiftDto(shift is null ? null : ToDto(shift, 0), source);
    }

    /// <summary>Employee, then department, then company default. One query covers the first two.</summary>
    private async Task<(Shift? Shift, string Source)> ResolveAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _shiftRepository.GetEmployeeWithShiftsAsync(employeeId, ct);
        if (employee is null) return (null, "Employee not found");

        if (employee.Shift is not null) return (employee.Shift, "Employee");
        if (employee.Department?.Shift is not null) return (employee.Department.Shift, "Department");

        var companyDefault = await _shiftRepository.GetDefaultAsync(employee.CompanyId, ct);
        return companyDefault is not null
            ? (companyDefault, "Company default")
            : (null, "No shift defined");
    }

    private static void Validate(SaveShiftDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("A shift needs a name.");

        if (dto.BreakMinutes < 0 || dto.BreakMinutes > 12 * 60)
            throw new InvalidOperationException("Break minutes must be between 0 and 720.");

        // Equal times would be a zero-length shift. Unequal in either direction is fine,
        // because EndTime before StartTime means the shift crosses midnight.
        if (dto.StartTime == dto.EndTime)
            throw new InvalidOperationException("A shift cannot start and end at the same time.");

        var length = dto.EndTime > dto.StartTime
            ? dto.EndTime - dto.StartTime
            : TimeSpan.FromHours(24) - (dto.StartTime - dto.EndTime);

        if (dto.BreakMinutes >= length.TotalMinutes)
            throw new InvalidOperationException("The break cannot be as long as the shift.");
    }

    private static ShiftDto ToDto(Shift s, int assignedEmployeeCount) => new(
        s.Id, s.CompanyId, s.Name, s.StartTime, s.EndTime, s.BreakMinutes,
        s.RestDays, s.IsDefault, s.CrossesMidnight, assignedEmployeeCount);
}
