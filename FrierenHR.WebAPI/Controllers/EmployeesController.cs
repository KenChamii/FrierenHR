using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Employee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    public EmployeesController(IEmployeeService employeeService) => _employeeService = employeeService;

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<List<EmployeeDto>>> GetByCompany([FromQuery] Guid companyId, CancellationToken ct) =>
        Ok(await _employeeService.GetByCompanyAsync(companyId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken ct)
    {
        var employee = await _employeeService.GetByIdAsync(id, ct);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpGet("{id:guid}/direct-reports")]
    public async Task<ActionResult<List<EmployeeDto>>> GetDirectReports(Guid id, CancellationToken ct) =>
        Ok(await _employeeService.GetDirectReportsAsync(id, ct));

    [HttpPost, AllowAnonymous]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto dto, CancellationToken ct)
    {
        try { var created = await _employeeService.CreateAsync(dto, ct); return CreatedAtAction(nameof(GetById), new { id = created.Id }, created); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> Update(Guid id, UpdateEmployeeDto dto, CancellationToken ct)
    {
        try { return Ok(await _employeeService.UpdateAsync(id, dto, ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }
}