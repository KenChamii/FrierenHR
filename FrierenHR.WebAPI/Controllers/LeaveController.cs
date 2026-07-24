using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Leave;
using FrierenHR.WebAPI.Common;
using FrierenHR.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/leave")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    public LeaveController(ILeaveService leaveService) => _leaveService = leaveService;

    [HttpPost("requests")]
    [HttpPost]
    public async Task<IActionResult> RequestLeave(CreateLeaveRequestDto dto, CancellationToken ct)
    {
        // You can only file leave for yourself; HRAdmin can file on behalf of someone (e.g. backdating a paper request).
        if (!User.IsSelfOrRole(dto.EmployeeId, "HRAdmin")) return Forbid();
        try { return Ok(await _leaveService.RequestLeaveAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("requests/{id:guid}/decide")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<LeaveRequestDto>> Decide(Guid id, DecideLeaveRequestDto dto, CancellationToken ct)
    {
        try { return Ok(await _leaveService.DecideAsync(id, dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("requests/by-employee/{employeeId:guid}")]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(employeeId, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _leaveService.GetByEmployeeAsync(employeeId, ct));
    }

    [HttpGet("requests/pending-for-approver/{approverId:guid}")]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetPendingForApprover(Guid approverId, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(approverId, "HRAdmin")) return Forbid();
        return Ok(await _leaveService.GetPendingForApproverAsync(approverId, ct));
    }

    [HttpGet("balances/{employeeId:guid}")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetBalances(Guid employeeId, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(employeeId, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _leaveService.GetBalancesAsync(employeeId, ct));
    }

    // Ops/admin action that recomputes accrual for every employee — not something any logged-in
    // user should be able to trigger on demand (it's effectively free-form DB load).
    [HttpPost("run-accrual")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> RunAccrual([FromServices] LeaveAccrualBackgroundService accrualService, CancellationToken ct)
    {
        await accrualService.RunAccrualForAllEmployeesAsync(ct);
        return Ok(new { message = "Accrual run completed." });
    }
}
