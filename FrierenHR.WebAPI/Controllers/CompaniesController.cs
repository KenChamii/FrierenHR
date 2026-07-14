using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Company;
using FrierenHR.Application.Features.Employee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    public CompaniesController(ICompanyService companyService) => _companyService = companyService;

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<List<CompanyDto>>> GetAll(CancellationToken ct) => Ok(await _companyService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken ct)
    {
        var company = await _companyService.GetByIdAsync(id, ct);
        return company is null ? NotFound() : Ok(company);
    }

    [HttpPost, AllowAnonymous]
    public async Task<ActionResult<CompanyDto>> Create(CreateCompanyDto dto, CancellationToken ct)
    {
        try { var created = await _companyService.CreateAsync(dto, ct); return CreatedAtAction(nameof(GetById), new { id = created.Id }, created); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("{companyId:guid}/departments")]
    public async Task<ActionResult<List<DepartmentDto>>> GetDepartments(Guid companyId, CancellationToken ct) =>
        Ok(await _companyService.GetDepartmentsAsync(companyId, ct));

    [HttpPost("{companyId:guid}/departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(Guid companyId, CreateDepartmentDto dto, CancellationToken ct) =>
        Ok(await _companyService.CreateDepartmentAsync(companyId, dto, ct));
}