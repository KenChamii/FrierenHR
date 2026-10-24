using FrierenHR.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Common.Security;
using FrierenHR.Application.Features.Onboarding;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRegistrationService _registrationService;
    private readonly TokenService _tokenService;

    public AuthController(
        IEmployeeRepository employeeRepository,
        IRegistrationService registrationService,
        TokenService tokenService)
    {
        _employeeRepository = employeeRepository;
        _registrationService = registrationService;
        _tokenService = tokenService;
    }

    public record LoginDto(string Email, string Password);
    public record LoginResultDto(
        string Token, Guid EmployeeId, string FullName, string Role, bool MustChangePassword = false);

    // Throttled per IP by the "login" policy in Program.cs, since this endpoint is anonymous.
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByEmailAsync(dto.Email, ct);
        if (employee is null || !employee.IsActive || !PasswordHasher.Verify(dto.Password, employee.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _tokenService.GenerateToken(employee);
        return Ok(new LoginResultDto(
            token, employee.Id, $"{employee.FirstName} {employee.LastName}", employee.Role.ToString(),
            employee.MustChangePassword));
    }

    // Company sign-up. Anonymous by necessity: there is no account yet.
    [HttpPost("register-company"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResultDto>> RegisterCompany(
        RegisterCompanyDto dto, CancellationToken ct)
    {
        try
        {
            var registered = await _registrationService.RegisterCompanyAsync(dto, ct);

            // Reloaded so the token is minted from the stored row rather than from the request.
            var admin = await _employeeRepository.GetByIdAsync(registered.EmployeeId, ct);
            if (admin is null) return StatusCode(StatusCodes.Status500InternalServerError);

            var token = _tokenService.GenerateToken(admin);
            return Ok(new LoginResultDto(
                token, admin.Id, registered.FullName, registered.Role.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            // A taken company code or email, or a password that fails the policy.
            return BadRequest(new { message = ex.Message });
        }
    }
}