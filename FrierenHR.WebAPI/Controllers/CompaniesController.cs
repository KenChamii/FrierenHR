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

    // Company sign-up — there's no logged-in user yet at this point, so this has to stay open.
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<List<CompanyDto>>> GetAll(CancellationToken ct) => Ok(await _companyService.GetAllAsync(ct));

    [HttpPost, AllowAnonymous]
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
