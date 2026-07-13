using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(FrierenHRDbContext context)
    {
        if (await context.Companies.AnyAsync()) return; // already seeded

        var companyA = new Company { Name = "Acme Corp", Code = "ACME" };
        var companyB = new Company { Name = "Globex Corp", Code = "GLOBEX" };
        context.Companies.AddRange(companyA, companyB);
        await context.SaveChangesAsync();
        // Add each company's CompanyRuleConfig rows here once Phase 1's
    }
}