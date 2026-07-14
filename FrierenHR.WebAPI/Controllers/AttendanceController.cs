using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Attendance;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    public AttendanceController(IAttendanceService attendanceService) => _attendanceService = attendanceService;

    [HttpPost("clock-in")]
    public async Task<ActionResult<AttendanceLogDto>> ClockIn(ClockInDto dto, CancellationToken ct)
    {
        try { return Ok(await _attendanceService.ClockInAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<AttendanceLogDto>> ClockOut(ClockOutDto dto, CancellationToken ct)
    {
        try { return Ok(await _attendanceService.ClockOutAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{employeeId:guid}")]
    public async Task<ActionResult<List<AttendanceLogDto>>> GetByEmployee(Guid employeeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        Ok(await _attendanceService.GetByEmployeeAsync(employeeId, from, to, ct));

    [HttpGet("{employeeId:guid}/ot")]
    public async Task<ActionResult<OtComputationResultDto>> ComputeOt(Guid employeeId, [FromQuery] DateTime date, CancellationToken ct) =>
        Ok(await _attendanceService.ComputeOtAsync(employeeId, date, ct));
}