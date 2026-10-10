using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Attendance.Corrections;

public interface IAttendanceCorrectionService
{
    Task<AttendanceCorrectionDto> RequestAsync(CreateAttendanceCorrectionDto dto, CancellationToken ct = default);

    /// <summary>Approving writes the attendance row; declining records the reason and nothing else.</summary>
    Task<AttendanceCorrectionDto> DecideAsync(
        Guid correctionId, DecideAttendanceCorrectionDto dto, CancellationToken ct = default);

    Task<List<AttendanceCorrectionDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    Task<List<AttendanceCorrectionDto>> GetPendingForApproverAsync(
        Guid approverEmployeeId, CancellationToken ct = default);
}
