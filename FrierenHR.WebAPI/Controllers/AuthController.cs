using FrierenHR.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Common.Security;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly TokenService _tokenService;
    public AuthController(IEmployeeRepository employeeRepository, TokenService tokenService)
    { _employeeRepository = employeeRepository; _tokenService = tokenService; }

    public record LoginDto(string Email, string Password);
    public record LoginResultDto(string Token, Guid EmployeeId, string FullName, string Role);

    [HttpPost("login"), AllowAnonymous]
    public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByEmailAsync(dto.Email, ct);
        if (employee is null || !employee.IsActive || !PasswordHasher.Verify(dto.Password, employee.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _tokenService.GenerateToken(employee);
        return Ok(new LoginResultDto(token, employee.Id, $"{employee.FirstName} {employee.LastName}", employee.Role.ToString()));
    }
}