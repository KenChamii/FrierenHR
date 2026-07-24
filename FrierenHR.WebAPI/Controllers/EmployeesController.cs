using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Employee;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    public EmployeesController(IEmployeeService employeeService) => _employeeService = employeeService;

    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetByCompany([FromQuery] Guid companyId, CancellationToken ct)
    {
        // Ignore whatever companyId the client sent and use the caller's own — otherwise any
        // logged-in employee could list another company's entire directory by guessing/passing
        // a different GUID.
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
}
