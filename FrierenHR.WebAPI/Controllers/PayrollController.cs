using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Payroll;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/payslips")]
[Authorize]
public class PayslipsController : ControllerBase
{
    private readonly IPayslipService _payslips;

    public PayslipsController(IPayslipService payslips) => _payslips = payslips;

    // GET /api/payslips/mine
    [HttpGet("mine")]
    public async Task<ActionResult<List<PayslipSummaryDto>>> Mine(CancellationToken ct)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null) return Forbid();
        return Ok(await _payslips.GetForEmployeeAsync(employeeId.Value, ct));
    }

    // GET /api/payslips/by-employee/{employeeId}
    [HttpGet("by-employee/{employeeId:guid}")]
    public async Task<ActionResult<List<PayslipSummaryDto>>> ByEmployee(Guid employeeId, CancellationToken ct)
    {
        // Pay is personal.
        if (!User.IsSelfOrRole(employeeId, "HRAdmin")) return Forbid();
        return Ok(await _payslips.GetForEmployeeAsync(employeeId, ct));
    }

    // GET /api/payslips/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PayslipDto>> GetById(Guid id, CancellationToken ct)
    {
        var payslip = await _payslips.GetByIdAsync(id, ct);
        if (payslip is null) return NotFound();
        if (!User.IsSelfOrRole(payslip.EmployeeId, "HRAdmin")) return Forbid();
        return Ok(payslip);
    }

    // GET /api/payslips/period?from=2026-09-16&to=2026-09-30
    [HttpGet("period")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<List<PayslipSummaryDto>>> Period(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _payslips.GetForPeriodAsync(companyId.Value, from, to, ct));
    }

    // POST /api/payslips/generate?from=2026-09-16&to=2026-09-30
    [HttpPost("generate")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<PayslipRunDto>> Generate(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();

        try { return Ok(await _payslips.GenerateAsync(companyId.Value, from, to, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Route("api/payroll-deductions")]
[Authorize(Roles = "HRAdmin")]
public class PayrollDeductionsController : ControllerBase
{
    private readonly IPayrollDeductionService _deductions;

    public PayrollDeductionsController(IPayrollDeductionService deductions) => _deductions = deductions;

    [HttpGet]
    public async Task<ActionResult<List<PayrollDeductionDto>>> Get(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _deductions.GetByCompanyAsync(companyId.Value, ct));
    }

    [HttpPost]
    public async Task<ActionResult<PayrollDeductionDto>> Save(
        SavePayrollDeductionDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();

        // The caller's own company, never the one in the body.
        dto = dto with { CompanyId = companyId.Value };

        try { return Ok(await _deductions.SaveAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();

        try { await _deductions.DeleteAsync(id, companyId.Value, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }
}
