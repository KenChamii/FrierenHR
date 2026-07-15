using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using Microsoft.EntityFrameworkCore;
using FrierenHR.Application.Common.Security;

namespace FrierenHR.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(FrierenHRDbContext context)
    {
        if (await context.Companies.AnyAsync()) return; // already seeded

        var companyA = new Company { Name = "Acme Corp", Code = "ACME" };
        var companyB = new Company { Name = "Globex Corp", Code = "GLOBEX" };
        context.Companies.AddRange(companyA, companyB);
  
        // Add each company's CompanyRuleConfig rows here once Phase 1's

        var admin = new Employee
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@acme.com",
            PasswordHash = PasswordHasher.Hash("Passw0rd!"),
            CompanyId = companyA.Id,
            HireDate = DateTime.UtcNow,
            Role = EmployeeRole.HRAdmin,
            IsActive = true
        };
        context.Employees.Add(admin);
        await context.SaveChangesAsync();
    }

}