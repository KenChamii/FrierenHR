namespace FrierenHR.Application.Common.Interfaces;

/// <summary>One holiday as an outside source reports it, before anybody has reviewed it.</summary>
public record HolidayFeedEntry(DateOnly Date, string Name);

/// <summary>
/// Fetches a year's public holidays for a country from somewhere outside the application.
///
/// An interface because the source is a judgement call that may change: no official API
/// publishes Philippine holidays, which are declared by proclamation, so whatever is used
/// is a convenience rather than an authority. Nothing it returns is saved without review.
/// </summary>
public interface IHolidayFeed
{
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> with a readable message when the
    /// source cannot be reached or returns something unexpected, so the screen can say that
    /// plainly rather than showing an empty list that looks like "no holidays this year".
    /// </summary>
    Task<List<HolidayFeedEntry>> GetForYearAsync(
        int year, string countryCode, CancellationToken ct = default);
}
