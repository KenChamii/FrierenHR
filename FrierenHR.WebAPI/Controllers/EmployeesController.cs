using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Features.Employee;
using FrierenHR.WebAPI.Common;
using FrierenHR.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly TokenService _tokenService;

    public EmployeesController(
        IEmployeeService employeeService,
        IEmployeeRepository employeeRepository,
        TokenService tokenService)
    {
        _employeeService = employeeService;
        _employeeRepository = employeeRepository;
        _tokenService = tokenService;
    }

    /// <summary>A replacement token, since the old one still says a change is required.</summary>
    public record ChangePasswordResultDto(string Token);

    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetByCompany([FromQuery] Guid companyId, CancellationToken ct)
    {
        // Ignore whatever companyId the client sent and use the caller's own.
        var callerCompanyId = User.GetCompanyId();
        if (callerCompanyId is null) return Forbid();
        return Ok(await _employeeService.GetByCompanyAsync(callerCompanyId.Value, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken ct)
    {
        var employee = await _employeeService.GetByIdAsync(id, ct);
        if (employee is null) return NotFound();
        if (employee.CompanyId != User.GetCompanyId()) return Forbid();
        return Ok(employee);
    }

    [HttpGet("{id:guid}/direct-reports")]
    public async Task<ActionResult<List<EmployeeDto>>> GetDirectReports(Guid id, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(id, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _employeeService.GetDirectReportsAsync(id, ct));
    }

    [HttpPost, Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto dto, CancellationToken ct)
    {
        var callerCompanyId = User.GetCompanyId();
        if (callerCompanyId is null) return Forbid();
        // Force the new hire into the HRAdmin's own company, regardless of what the request body claims.
        if (dto.CompanyId != callerCompanyId) dto = dto with { CompanyId = callerCompanyId.Value };

        try { var created = await _employeeService.CreateAsync(dto, ct); return CreatedAtAction(nameof(GetById), new { id = created.Id }, created); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<EmployeeDto>> Update(Guid id, UpdateEmployeeDto dto, CancellationToken ct)
    {
        var existing = await _employeeService.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();
        if (existing.CompanyId != User.GetCompanyId()) return Forbid();

        try { return Ok(await _employeeService.UpdateAsync(id, dto, ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }

    // Issues a new temporary password and returns it once, for somebody who cannot sign in.
    [HttpPost("{id:guid}/reset-password"), Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<ResetPasswordResultDto>> ResetPassword(Guid id, CancellationToken ct)
    {
        var callerCompanyId = User.GetCompanyId();
        if (callerCompanyId is null) return Forbid();

        // Resetting your own password this way would hand you a credential without proving you know the current one.
        if (User.GetEmployeeId() == id)
            return BadRequest(new { message = "Change your own password from your profile instead." });

        try { return Ok(await _employeeService.ResetPasswordAsync(id, callerCompanyId.Value, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // Self-service — you change your own password by proving you know the current one.
    [HttpPost("{id:guid}/change-password")]
    public async Task<ActionResult<ChangePasswordResultDto>> ChangePassword(
        Guid id, ChangePasswordDto dto, CancellationToken ct)
    {
        if (User.GetEmployeeId() != id) return Forbid();

        try
        {
            await _employeeService.ChangePasswordAsync(id, dto, ct);

            // A fresh token, because the one in hand still carries mustChangePassword and would keep the account locked out of everything it just earned access to.
            var employee = await _employeeRepository.GetByIdAsync(id, ct);
            if (employee is null) return NotFound();

            return Ok(new ChangePasswordResultDto(_tokenService.GenerateToken(employee)));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
