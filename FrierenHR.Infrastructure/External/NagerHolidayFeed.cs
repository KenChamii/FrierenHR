using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FrierenHR.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrierenHR.Infrastructure.External;

/// <summary>Reads a year's public holidays from the Nager.Date public API.</summary>
public class NagerHolidayFeed : IHolidayFeed
{
    private readonly HttpClient _http;
    private readonly ILogger<NagerHolidayFeed> _logger;

    public NagerHolidayFeed(HttpClient http, ILogger<NagerHolidayFeed> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<HolidayFeedEntry>> GetForYearAsync(
        int year, string countryCode, CancellationToken ct = default)
    {
        var path = $"api/v3/PublicHolidays/{year}/{countryCode.ToUpperInvariant()}";

        List<NagerHoliday>? holidays;
        try
        {
            holidays = await _http.GetFromJsonAsync<List<NagerHoliday>>(path, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Logged with the detail, surfaced without it.
            _logger.LogWarning(ex, "Holiday feed request failed for {Year} {Country}.", year, countryCode);

            throw new InvalidOperationException(
                "Could not reach the holiday service. Check the connection, or paste the list in by hand.");
        }

        if (holidays is null)
            throw new InvalidOperationException($"The holiday service returned nothing for {year}.");

        return holidays
            .Where(h => !string.IsNullOrWhiteSpace(h.Date) && !string.IsNullOrWhiteSpace(h.DisplayName))
            .Select(h => new HolidayFeedEntry(DateOnly.Parse(h.Date!), h.DisplayName!.Trim()))
            .OrderBy(h => h.Date)
            .ToList();
    }

    /// <summary>The fields of the response this uses. The rest is ignored on purpose.</summary>
    private sealed record NagerHoliday
    {
        [JsonPropertyName("date")]
        public string? Date { get; init; }

        /// <summary>The name in the country's own language, which is what people recognise.</summary>
        [JsonPropertyName("localName")]
        public string? LocalName { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        public string? DisplayName => string.IsNullOrWhiteSpace(LocalName) ? Name : LocalName;
    }
}
