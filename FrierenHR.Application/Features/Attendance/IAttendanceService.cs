using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Attendance;

public interface IAttendanceService
{
    Task<AttendanceLogDto> ClockInAsync(ClockInDto dto, CancellationToken ct = default);
    Task<AttendanceLogDto> ClockOutAsync(ClockOutDto dto, CancellationToken ct = default);
    Task<AttendanceLogDto> LogShiftAsync(LogShiftDto dto, CancellationToken ct = default);

    /// <summary>
    /// Saves several days at once, in one transaction. A day already logged is updated; the
    /// same rules as a single shift apply to every day, so one bad day rejects the whole week
    /// rather than leaving it half saved.
    /// </summary>
    Task<List<AttendanceLogDto>> LogWeekAsync(LogWeekDto dto, CancellationToken ct = default);
    Task<AttendanceLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<List<AttendanceLogDto>> GetByEmployeeAsync(Guid employeeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<OtComputationResultDto> ComputeOtAsync(Guid employeeId, DateTime date, CancellationToken ct = default);
}