using System.Text.Json;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Common.Security;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine;
using FrierenHR.Core.RulesEngine.Models;
using CompanyEntity = FrierenHR.Core.Entities.Company;
using EmployeeEntity = FrierenHR.Core.Entities.Employee;

namespace FrierenHR.Application.Features.Onboarding;

/// <summary>Sign-up for a new company.</summary>
public class RegistrationService : IRegistrationService
{
    private const string DefaultShiftName = "Standard day";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IShiftRepository _shifts;
    private readonly IRuleConfigRepository _rules;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrationService(
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IShiftRepository shifts,
        IRuleConfigRepository rules,
        IUnitOfWork unitOfWork)
    {
        _companies = companies;
        _employees = employees;
        _shifts = shifts;
        _rules = rules;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisteredCompanyDto> RegisterCompanyAsync(
        RegisterCompanyDto dto, CancellationToken ct = default)
    {
        var companyName = (dto.CompanyName ?? string.Empty).Trim();
        var companyCode = (dto.CompanyCode ?? string.Empty).Trim().ToUpperInvariant();
        var email = (dto.Email ?? string.Empty).Trim();
        var firstName = (dto.FirstName ?? string.Empty).Trim();
        var lastName = (dto.LastName ?? string.Empty).Trim();

        if (companyName.Length == 0) throw new InvalidOperationException("Company name is required.");
        if (companyCode.Length == 0) throw new InvalidOperationException("Company code is required.");
        if (firstName.Length == 0 || lastName.Length == 0)
            throw new InvalidOperationException("Your first and last name are required.");
        if (email.Length == 0) throw new InvalidOperationException("Email is required.");

        // Checked before the transaction so the common mistakes come back as a clear message rather than as a unique-index violation.
        if (await _companies.GetByCodeAsync(companyCode, ct) is not null)
            throw new InvalidOperationException($"Company code '{companyCode}' is already in use.");

        if (await _employees.GetByEmailAsync(email, ct) is not null)
            throw new InvalidOperationException($"Email '{email}' is already registered.");

        PasswordPolicy.Validate(dto.Password);

        return await _unitOfWork.ExecuteAsync(async token =>
        {
            var company = new CompanyEntity { Name = companyName, Code = companyCode };
            await _companies.AddAsync(company, token);
            await _companies.SaveChangesAsync(token);

            var admin = new EmployeeEntity
            {
                CompanyId = company.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                HireDate = DateTime.UtcNow.Date,
                // Forced, never read from the request: this endpoint is anonymous.
                Role = EmployeeRole.HRAdmin,
            };
            await _employees.AddAsync(admin, token);
            await _employees.SaveChangesAsync(token);

            await AddStarterConfiguration(company.Id, token);

            return new RegisteredCompanyDto(
                company.Id, company.Name, company.Code,
                admin.Id, $"{admin.FirstName} {admin.LastName}", admin.Email, admin.Role);
        }, ct);
    }

    /// <summary>A company with no shift and no rules looks broken on first use: nothing knows the working day.</summary>
    private async Task AddStarterConfiguration(Guid companyId, CancellationToken ct)
    {
        await _shifts.AddAsync(new Shift
        {
            CompanyId = companyId,
            Name = DefaultShiftName,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
            BreakMinutes = 60,
            RestDays = WeekDays.Weekend,
            IsDefault = true,
        }, ct);
        await _shifts.SaveChangesAsync(ct);

        foreach (var (ruleType, priority, rule) in StarterRules())
        {
            await _rules.AddAsync(new CompanyRuleConfig
            {
                CompanyId = companyId,
                RuleType = ruleType,
                Priority = priority,
                RuleJson = JsonSerializer.Serialize(rule, JsonOptions),
            }, ct);
        }

        await _rules.SaveChangesAsync(ct);
    }

    /// <summary>Built as objects and serialised, rather than written as JSON strings.</summary>
    private static List<(RuleType Type, int Priority, RuleDefinition Rule)> StarterRules() =>
    [
        // Accrue 1.25 days a month to a 15-day ceiling, the common Philippine leave shape.
        (RuleType.LeaveAccrual, 0, new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveAccrual),
            Conditions = [new RuleCondition { Field = "tenureMonths", Op = ">=", Value = 6 }],
            Action = new RuleAction { Type = "AccrueDays", Amount = 1.25m, Cap = 15m },
        }),

        // A single day clears itself; anything longer goes to a manager. No rule is written
        // for the longer case: when nothing matches, LeaveService already falls back to
        // requiring approval, which is the safe default and the one worth relying on.
        (RuleType.LeaveApproval, 0, new RuleDefinition
        {
            RuleType = nameof(RuleType.LeaveApproval),
            Conditions = [new RuleCondition { Field = "days", Op = "<=", Value = 1 }],
            Action = new RuleAction { Type = "AutoApprove" },
        }),

        (RuleType.OTMultiplier, 0, new RuleDefinition
        {
            RuleType = nameof(RuleType.OTMultiplier),
            Conditions = [],
            Action = new RuleAction { Type = "OTMultiplier", Amount = 1.25m },
        }),

        (RuleType.LateGracePeriod, 0, new RuleDefinition
        {
            RuleType = nameof(RuleType.LateGracePeriod),
            Conditions = [],
            Action = new RuleAction { Type = "LateGraceMinutes", Amount = 15m },
        }),
    ];
}
