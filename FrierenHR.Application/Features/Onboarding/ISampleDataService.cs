namespace FrierenHR.Application.Features.Onboarding;

public interface ISampleDataService
{
    /// <summary>True while the company is empty enough for <see cref="LoadAsync"/> to accept it.</summary>
    Task<bool> IsAvailableAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>Fills a company that has nothing in it yet with departments, staff, leave, attendance and holidays.</summary>
    Task LoadAsync(Guid companyId, CancellationToken ct = default);
}
