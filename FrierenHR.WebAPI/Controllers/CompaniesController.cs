using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Company;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    public CompaniesController(ICompanyService companyService) => _companyService = companyService;

    // Was anonymous, to populate a sign-up dropdown. That published every tenant's name,
    // code and headcount to anyone who asked. Sign-up now creates its own company through
    // POST /api/auth/register-company and needs no list.
    [HttpGet, Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<List<CompanyDto>>> GetAll(CancellationToken ct) => Ok(await _companyService.GetAllAsync(ct));

    // Creating a company without its first HRAdmin leaves a tenant nobody can log into, so
    // this is no longer the sign-up route and requires an existing administrator.
    [HttpPost, Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<CompanyDto>> Create(CreateCompanyDto dto, CancellationToken ct)
    {
        try { var created = await _companyService.CreateAsync(dto, ct); return CreatedAtAction(nameof(GetById), new { id = created.Id }, created); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("{id:guid}"), Authorize]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken ct)
    {
        if (id != User.GetCompanyId()) return Forbid();
        var company = await _companyService.GetByIdAsync(id, ct);
        return company is null ? NotFound() : Ok(company);
    }

    [HttpGet("{companyId:guid}/departments"), Authorize]
    public async Task<ActionResult<List<DepartmentDto>>> GetDepartments(Guid companyId, CancellationToken ct)
    {
        // Same pattern as Employees: ignore whatever companyId is in the URL and use the
        // caller's own, so nobody can browse another company's department list.
        var callerCompanyId = User.GetCompanyId();
        if (callerCompanyId is null) return Forbid();
        return Ok(await _companyService.GetDepartmentsAsync(callerCompanyId.Value, ct));
    }

    [HttpPost("{companyId:guid}/departments"), Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(Guid companyId, CreateDepartmentDto dto, CancellationToken ct)
    {
        var callerCompanyId = User.GetCompanyId();
        if (callerCompanyId is null) return Forbid();
        return Ok(await _companyService.CreateDepartmentAsync(callerCompanyId.Value, dto, ct));
    }
}
