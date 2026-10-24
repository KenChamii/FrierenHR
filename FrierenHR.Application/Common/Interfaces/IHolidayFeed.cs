namespace FrierenHR.Application.Common.Interfaces;

/// <summary>One holiday as an outside source reports it, before anybody has reviewed it.</summary>
public record HolidayFeedEntry(DateOnly Date, string Name);

/// <summary>Fetches a year's public holidays for a country from somewhere outside the application.</summary>
public interface IHolidayFeed
{
    /// <summary>Throws <see cref="InvalidOperationException"/> with a readable message when the source cannot be reached or returns something unexpected.</summary>
    Task<List<HolidayFeedEntry>> GetForYearAsync(
        int year, string countryCode, CancellationToken ct = default);
}
