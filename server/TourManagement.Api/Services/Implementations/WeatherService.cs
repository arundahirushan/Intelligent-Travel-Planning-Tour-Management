using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Weather;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

public class WeatherService : IWeatherService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;

    public WeatherService(AppDbContext db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
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

        try
        {
            // 1. Resolve coordinates
            decimal? lat = null;
            decimal? lon = null;

            var dbDest = await _db.Destinations.FirstOrDefaultAsync(d => d.Name == destinationName);
            if (dbDest != null && dbDest.Latitude.HasValue && dbDest.Longitude.HasValue)
            {
                lat = dbDest.Latitude.Value;
                lon = dbDest.Longitude.Value;
            }
            else
            {
                // Geocode using Open-Meteo
                // Add Sri Lanka to context to avoid ambiguous matches globally
                var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(destinationName + " Sri Lanka")}&count=1&language=en&format=json";
                var geoResp = await _httpClient.GetAsync(geoUrl);
                
                if (geoResp.IsSuccessStatusCode)
                {
                    var geoStr = await geoResp.Content.ReadAsStringAsync();
                    var geoDoc = JsonDocument.Parse(geoStr);
                    if (geoDoc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                    {
                        var firstMatch = results[0];
                        
                        // Check if it's actually in Sri Lanka
                        if (firstMatch.TryGetProperty("country", out var countryProp) && countryProp.GetString() == "Sri Lanka")
                        {
                            if (firstMatch.TryGetProperty("latitude", out var latProp) && firstMatch.TryGetProperty("longitude", out var lonProp))
                            {
                                lat = (decimal)latProp.GetDouble();
                                lon = (decimal)lonProp.GetDouble();
                                
                                // Optionally save it back to DB? 
                                // Instructions: "Do not redesign traveler screens... Reuse stored coordinates if available".
                                // We won't save it back to avoid unintended side effects, just use it.
                            }
                        }
                    }
                }
            }

            if (!lat.HasValue || !lon.HasValue)
            {
                result.Status = "Unavailable_LocationNotFound";
                result.Advisory = "Weather service unavailable; location could not be reliably resolved.";
                return result;
            }

            // 2. Fetch weather for date
            // Open-Meteo forecast limit is typically 14 days out. 
            // Past dates might need historical API, but we just use forecast.
            
            var dateStr = result.Date;
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum&timezone=Asia%2FColombo&start_date={dateStr}&end_date={dateStr}";
            
            var weatherResp = await _httpClient.GetAsync(url);
            if (weatherResp.IsSuccessStatusCode)
            {
                var weatherStr = await weatherResp.Content.ReadAsStringAsync();
                var weatherDoc = JsonDocument.Parse(weatherStr);
                
                if (weatherDoc.RootElement.TryGetProperty("daily", out var daily))
                {
                    if (daily.TryGetProperty("time", out var timeArray) && timeArray.GetArrayLength() > 0)
                    {
                        if (daily.TryGetProperty("weather_code", out var wc) && wc.GetArrayLength() > 0)
                        {
                            var code = wc[0].GetInt32();
                            result.WeatherCode = code;
                            
                            // 0-3: Clear/Cloudy (favorable)
                            // 45-48: Fog
                            // 51-67: Drizzle/Rain
                            // 71-77: Snow
                            // 80-82: Rain showers
                            // 95-99: Thunderstorm
                            
                            if (code <= 3) result.Advisory = "Generally clear conditions are forecast.";
                            else if (code >= 51 && code <= 67 || code >= 80 && code <= 82) result.Advisory = "Rain is forecast; allow for wet conditions.";
                            else if (code >= 95) result.Advisory = "Thunderstorms are forecast; expect significant disruptions.";
                            else result.Advisory = "Mixed or cloudy conditions are forecast.";
                        }
                        
                        if (daily.TryGetProperty("temperature_2m_max", out var tmax) && tmax.GetArrayLength() > 0)
                            result.MaxTemperatureC = tmax[0].GetDouble();
                            
                        if (daily.TryGetProperty("temperature_2m_min", out var tmin) && tmin.GetArrayLength() > 0)
                            result.MinTemperatureC = tmin[0].GetDouble();
                            
                        if (daily.TryGetProperty("precipitation_sum", out var precip) && precip.GetArrayLength() > 0)
                            result.PrecipitationSumMm = precip[0].GetDouble();
                            
                        result.Status = "Available";
                    }
                    else
                    {
                        result.Status = "Unavailable_DateOutOfRange";
                        result.Advisory = "Forecast is not yet available for this date.";
                    }
                }
                else
                {
                    result.Status = "Unavailable_ProviderError";
                    result.Advisory = "Weather service unavailable; check closer to departure.";
                }
            }
            else if (weatherResp.StatusCode == System.Net.HttpStatusCode.BadRequest && weatherResp.Content.ReadAsStringAsync().Result.Contains("out of range"))
            {
                result.Status = "Unavailable_DateOutOfRange";
                result.Advisory = "Forecast is not yet available for this date.";
            }
            else
            {
                result.Status = "Unavailable_ProviderError";
                result.Advisory = "Weather service unavailable; provider error.";
            }
        }
        catch (Exception)
        {
            result.Status = "Unavailable_ProviderError";
            result.Advisory = "Weather service unavailable; network error.";
        }

        return result;
    }
}

