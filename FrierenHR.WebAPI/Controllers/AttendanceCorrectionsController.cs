using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Attendance.Corrections;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/attendance-corrections")]
[Authorize]
public class AttendanceCorrectionsController : ControllerBase
{
    private readonly IAttendanceCorrectionService _corrections;
    public AttendanceCorrectionsController(IAttendanceCorrectionService corrections) => _corrections = corrections;

    // POST /api/attendance-corrections — raise a correction for a day you missed.
    [HttpPost]
    public async Task<ActionResult<AttendanceCorrectionDto>> Request(
        CreateAttendanceCorrectionDto dto, CancellationToken ct)
    {
        var caller = User.GetEmployeeId();
        if (caller is null) return Forbid();

        // Only HR may file on someone else's behalf, for a person who cannot reach the system.
        if (dto.EmployeeId != caller && User.GetRole() != "HRAdmin") return Forbid();

        try { return Ok(await _corrections.RequestAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // GET /api/attendance-corrections/mine — your own history, decided or not.
    [HttpGet("mine")]
    public async Task<ActionResult<List<AttendanceCorrectionDto>>> Mine(CancellationToken ct)
    {
        var caller = User.GetEmployeeId();
        if (caller is null) return Forbid();
        return Ok(await _corrections.GetByEmployeeAsync(caller.Value, ct));
    }

    // GET /api/attendance-corrections/pending — your inbox: reports for a manager, the whole
    // company for HR.
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<List<AttendanceCorrectionDto>>> Pending(CancellationToken ct)
    {
        var caller = User.GetEmployeeId();
        if (caller is null) return Forbid();

        try { return Ok(await _corrections.GetPendingForApproverAsync(caller.Value, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/decide")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<AttendanceCorrectionDto>> Decide(
        Guid id, DecideAttendanceCorrectionDto dto, CancellationToken ct)
    {
        var caller = User.GetEmployeeId();
        if (caller is null) return Forbid();
        // The decision is attributed to the caller, never to whoever the body names.
        dto = dto with { DecidedByEmployeeId = caller.Value };

        try { return Ok(await _corrections.DecideAsync(id, dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
