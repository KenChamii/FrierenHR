using FrierenHR.Application.Features.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Data;

/// <summary>Loads <see cref="DemoDataSeeder"/>'s data into a company someone signed up for.</summary>
public class SampleDataService : ISampleDataService
{
    private readonly FrierenHRDbContext _context;

    public SampleDataService(FrierenHRDbContext context) => _context = context;

    public async Task<bool> IsAvailableAsync(Guid companyId, CancellationToken ct = default)
    {
        // Only the administrator sign-up created may exist.
        var hasRecords =
            await _context.Departments.AnyAsync(d => d.CompanyId == companyId, ct)
            || await _context.Holidays.AnyAsync(h => h.CompanyId == companyId, ct)
            || await _context.Shifts.CountAsync(s => s.CompanyId == companyId, ct) > 1
            || await _context.Employees.CountAsync(e => e.CompanyId == companyId, ct) > 1;

        return !hasRecords;
    }

    public async Task LoadAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new InvalidOperationException("Company not found.");

        if (!await IsAvailableAsync(companyId, ct))
            throw new InvalidOperationException(
                "Sample data only loads into a company with no departments, holidays or staff yet.");

        var admin = await _context.Employees
            .FirstAsync(e => e.CompanyId == companyId, ct);

        var password = "Sample-" + Guid.NewGuid().ToString("N") + "1";
        var label = new string(company.Code.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        var domain = $"{(label.Length > 0 ? label : "company")}.sample.{DemoDataSeeder.EmailDomain}";

        await DemoDataSeeder.PopulateAsync(_context, companyId, admin.Id, password, domain, ct);
    }
}
