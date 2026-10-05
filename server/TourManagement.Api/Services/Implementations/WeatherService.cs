using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Weather;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

public class WeatherService : IWeatherService
{
    // One timezone for both "what is today" and the forecast request, so they always agree.
    private const string WeatherTimeZoneId = "Asia/Colombo";

    // Open-Meteo forecasts 16 days including today: today .. today + 15.
    private const int LastForecastDayOffset = 15;

    private const string OutOfRangeAdvisory = "Forecast is not yet available for this date.";
    private const string ProviderErrorAdvisory = "Weather forecast is temporarily unavailable. Please check closer to departure.";

    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly TimeProvider _time;

    // timeProvider is optional so tests can fix "today"; the app uses the real clock.
    public WeatherService(AppDbContext db, HttpClient httpClient, ILogger<WeatherService> logger, TimeProvider? timeProvider = null)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
        _time = timeProvider ?? TimeProvider.System;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<WeatherResultDto> GetWeatherAsync(string destinationName, DateTime date)
    {
        var result = new WeatherResultDto
        {
            Destination = destinationName,
            Date = date.ToString("yyyy-MM-dd"),
            RetrievalTime = DateTime.UtcNow
        };

        // Dates beyond the forecast window never need a provider call.
        // Past dates keep the existing behaviour: ask the provider and let it decide.
        if ((date.Date - TodayInWeatherTimeZone()).Days > LastForecastDayOffset)
        {
            result.Status = "Unavailable_DateOutOfRange";
            result.Advisory = OutOfRangeAdvisory;
            return result;
        }

        try
        {
            var (lat, lon, districtName) = await ResolveCoordinatesAsync(destinationName);
            if (!lat.HasValue || !lon.HasValue)
            {
                result.Status = "Unavailable_LocationNotFound";
                result.Advisory = "Weather service unavailable; location could not be reliably resolved.";
                return result;
            }
            
            result.DistrictName = districtName ?? string.Empty;

            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum&timezone={Uri.EscapeDataString(WeatherTimeZoneId)}&start_date={result.Date}&end_date={result.Date}";
            var weatherResp = await _httpClient.GetAsync(url);
            var body = await weatherResp.Content.ReadAsStringAsync();

            if (weatherResp.IsSuccessStatusCode)
            {
                FillFromForecast(result, body);
            }
            else if (weatherResp.StatusCode == System.Net.HttpStatusCode.BadRequest && IsOutOfRangeError(body))
            {
                result.Status = "Unavailable_DateOutOfRange";
                result.Advisory = OutOfRangeAdvisory;
            }
            else
            {
                // Raw provider text goes to the log only, never to the traveler.
                _logger.LogWarning("Weather provider returned {StatusCode} for {Destination} {Date}: {Body}",
                    (int)weatherResp.StatusCode, destinationName, result.Date, body);
                result.Status = "Unavailable_ProviderError";
                result.Advisory = ProviderErrorAdvisory;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            _logger.LogWarning(ex, "Weather provider could not be reached for {Destination} {Date}.", destinationName, result.Date);
            result.Status = "Unavailable_ProviderError";
            result.Advisory = ProviderErrorAdvisory;
        }
        catch (Exception ex)
        {
            // Weather is advisory, so the proposal must not fail. The full stack trace is logged.
            _logger.LogError(ex, "Unexpected error getting weather for {Destination} {Date}.", destinationName, result.Date);
            result.Status = "Unavailable_ProviderError";
            result.Advisory = ProviderErrorAdvisory;
        }

        return result;
    }

    // Today's date in the weather timezone. Sri Lanka has no daylight saving, so the fixed
    // +05:30 fallback is exact if the timezone database lacks the IANA id.
    private DateTime TodayInWeatherTimeZone()
    {
        var utcNow = _time.GetUtcNow();
        return TimeZoneInfo.TryFindSystemTimeZoneById(WeatherTimeZoneId, out var tz)
            ? TimeZoneInfo.ConvertTime(utcNow, tz).Date
            : utcNow.ToOffset(TimeSpan.FromHours(5.5)).Date;
    }

    private async Task<(decimal? lat, decimal? lon, string? districtName)> ResolveCoordinatesAsync(string destinationName)
    {
        var dbDest = await _db.Destinations.FirstOrDefaultAsync(d => d.Name == destinationName);
        if (dbDest == null)
            return (null, null, null);

        // Authoritative mapping checks Region first
        if (TourManagement.Api.Common.Constants.SriLankaDistricts.Map.TryGetValue(dbDest.Region, out var coords))
        {
            return (coords.Latitude, coords.Longitude, dbDest.Region);
        }

        // If it's a recognized district but has null coordinates, the above handles it.
        // If not a recognized district, we use DB coordinates if they happen to exist.
        if (dbDest.Latitude.HasValue && dbDest.Longitude.HasValue)
            return (dbDest.Latitude.Value, dbDest.Longitude.Value, null);

        return (null, null, null);
    }

    // Open-Meteo's real wording: "Parameter 'start_date' is out of allowed range from ... to ..."
    private static bool IsOutOfRangeError(string body) =>
        body.Contains("out of allowed range", StringComparison.OrdinalIgnoreCase)
        || body.Contains("out of range", StringComparison.OrdinalIgnoreCase);

    private static void FillFromForecast(WeatherResultDto result, string body)
    {
        var root = JsonDocument.Parse(body).RootElement;

        if (!root.TryGetProperty("daily", out var daily))
        {
            result.Status = "Unavailable_ProviderError";
            result.Advisory = ProviderErrorAdvisory;
            return;
        }

        if (!daily.TryGetProperty("time", out var times) || times.GetArrayLength() == 0)
        {
            result.Status = "Unavailable_DateOutOfRange";
            result.Advisory = OutOfRangeAdvisory;
            return;
        }

        if (daily.TryGetProperty("weather_code", out var wc) && wc.GetArrayLength() > 0 && wc[0].ValueKind == JsonValueKind.Number)
        {
            var code = wc[0].GetInt32();
            result.WeatherCode = code;

            // 0-3 clear/cloudy, 51-67 drizzle/rain, 80-82 showers, 95+ thunderstorm.
            if (code <= 3) result.Advisory = "Generally clear conditions are forecast.";
            else if (code >= 51 && code <= 67 || code >= 80 && code <= 82) result.Advisory = "Rain is forecast; allow for wet conditions.";
            else if (code >= 95) result.Advisory = "Thunderstorms are forecast; expect significant disruptions.";
            else result.Advisory = "Mixed or cloudy conditions are forecast.";
        }

        result.MaxTemperatureC = ReadFirstNumber(daily, "temperature_2m_max");
        result.MinTemperatureC = ReadFirstNumber(daily, "temperature_2m_min");
        result.PrecipitationSumMm = ReadFirstNumber(daily, "precipitation_sum"); // 0 is a valid value
        result.Status = "Available";
    }

    // Returns the first value of a daily array, or null when it is missing or null.
    private static double? ReadFirstNumber(JsonElement daily, string name) =>
        daily.TryGetProperty(name, out var arr) && arr.GetArrayLength() > 0 && arr[0].ValueKind == JsonValueKind.Number
            ? arr[0].GetDouble()
            : null;
}
