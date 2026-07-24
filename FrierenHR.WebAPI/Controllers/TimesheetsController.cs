using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Timesheet;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/timesheets")]
[Authorize]
public class TimesheetsController : ControllerBase
{
    private readonly ITimesheetService _timesheetService;
    public TimesheetsController(ITimesheetService timesheetService) => _timesheetService = timesheetService;

    [HttpPost("submit")]
    public async Task<ActionResult<TimesheetDto>> Submit(SubmitTimesheetDto dto, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(dto.EmployeeId, "HRAdmin")) return Forbid();
        try { return Ok(await _timesheetService.SubmitAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/decide"), Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<TimesheetDto>> Decide(Guid id, DecideTimesheetDto dto, CancellationToken ct)
    {
        try { return Ok(await _timesheetService.DecideAsync(id, dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("by-employee/{employeeId:guid}")]
    public async Task<ActionResult<List<TimesheetDto>>> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(employeeId, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _timesheetService.GetByEmployeeAsync(employeeId, ct));
    }

    [HttpGet("by-employee/{employeeId:guid}/week")]
    public async Task<ActionResult<TimesheetDto?>> GetForWeek(Guid employeeId, [FromQuery] DateTime weekStartDate, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(employeeId, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _timesheetService.GetForWeekAsync(employeeId, weekStartDate, ct));
    }

    [HttpGet("pending-for-approver/{approverId:guid}")]
    public async Task<ActionResult<List<TimesheetDto>>> GetPendingForApprover(Guid approverId, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(approverId, "HRAdmin")) return Forbid();
        return Ok(await _timesheetService.GetPendingForApproverAsync(approverId, ct));
    }
}
