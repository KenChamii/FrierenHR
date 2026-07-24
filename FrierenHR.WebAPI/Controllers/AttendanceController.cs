using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Attendance;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    public AttendanceController(IAttendanceService attendanceService) => _attendanceService = attendanceService;

    [HttpPost("clock-in")]
    public async Task<ActionResult<AttendanceLogDto>> ClockIn(ClockInDto dto, CancellationToken ct)
    {
        // Employees can only clock themselves in — an HRAdmin correcting someone else's
        // record is a separate "adjust attendance" feature, not this self-service endpoint.
        if (!User.IsSelfOrRole(dto.EmployeeId, "HRAdmin")) return Forbid();
        try { return Ok(await _attendanceService.ClockInAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<AttendanceLogDto>> ClockOut(ClockOutDto dto, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(dto.EmployeeId, "HRAdmin")) return Forbid();
        try { return Ok(await _attendanceService.ClockOutAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("log-shift")]
    public async Task<ActionResult<AttendanceLogDto>> LogShift(LogShiftDto dto, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(dto.EmployeeId, "HRAdmin")) return Forbid();
        try { return Ok(await _attendanceService.LogShiftAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var log = await _attendanceService.GetByIdAsync(id, ct);
        if (log is null) return NotFound();
        if (!User.IsSelfOrRole(log.EmployeeId, "HRAdmin")) return Forbid();

        try { await _attendanceService.DeleteAsync(id, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{employeeId:guid}")]
    public async Task<ActionResult<List<AttendanceLogDto>>> GetByEmployee(Guid employeeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        // Self, or a Manager/HRAdmin looking up someone else's record.
        if (!User.IsSelfOrRole(employeeId, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _attendanceService.GetByEmployeeAsync(employeeId, from, to, ct));
    }

    [HttpGet("{employeeId:guid}/ot")]
    public async Task<ActionResult<OtComputationResultDto>> ComputeOt(Guid employeeId, [FromQuery] DateTime date, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(employeeId, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _attendanceService.ComputeOtAsync(employeeId, date, ct));
    }
}
