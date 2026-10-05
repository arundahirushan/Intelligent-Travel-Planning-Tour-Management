using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

namespace TourManagement.Api.Tests.Services;

public class WeatherServiceTests : IDisposable
{
    // The provider's real rejection text for a date past its forecast window.
    private const string ProviderOutOfRangeBody =
        "{\"reason\":\"Parameter 'start_date' is out of allowed range from 2026-07-03 to 2026-10-19\",\"error\":true}";

    private const string ForecastBody =
        "{\"daily\":{\"time\":[\"2026-10-10\"],\"weather_code\":[1],\"temperature_2m_max\":[29.5],\"temperature_2m_min\":[21.0],\"precipitation_sum\":[0.0]}}";

    private readonly AppDbContext _db;

    public WeatherServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        // Stored coordinates, so no geocoding request is made.
        _db.Destinations.Add(new Destination { Id = 1, Name = "Kandy", Latitude = 7.2906m, Longitude = 80.6337m });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // Fixed clock: 2026-10-04 06:00 UTC = 11:30 in Colombo, so "today" is 2026-10-04.
    private class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedClock(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private class FakeProvider : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;
        public int Calls { get; private set; }
        public FakeProvider(Func<HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_respond());
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body) };

    private WeatherService Create(FakeProvider provider, DateTimeOffset? now = null) =>
        new(_db, new HttpClient(provider), NullLogger<WeatherService>.Instance,
            new FixedClock(now ?? new DateTimeOffset(2026, 10, 4, 6, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task LastSupportedDate_TodayPlus15_CallsProvider()
    {
        var provider = new FakeProvider(() => Json(HttpStatusCode.OK, ForecastBody));

        var result = await Create(provider).GetWeatherAsync("Kandy", new DateTime(2026, 10, 19));

        Assert.Equal("Available", result.Status);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task DateAfterLastSupported_ReturnsOutOfRange_WithoutCallingProvider()
    {
        var provider = new FakeProvider(() => Json(HttpStatusCode.OK, ForecastBody));

        var result = await Create(provider).GetWeatherAsync("Kandy", new DateTime(2026, 10, 20));

        Assert.Equal("Unavailable_DateOutOfRange", result.Status);
        Assert.Equal("Forecast is not yet available for this date.", result.Advisory);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task October21_IsOutOfRange_OnOctober4()
    {
        var provider = new FakeProvider(() => Json(HttpStatusCode.OK, ForecastBody));

        var result = await Create(provider).GetWeatherAsync("Kandy", new DateTime(2026, 10, 21));

        Assert.Equal("Unavailable_DateOutOfRange", result.Status);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Today_UsesColomboDate_NotUtcDate()
    {
        // 2026-10-03 20:00 UTC is already 2026-10-04 in Colombo, so 10-19 is still today + 15.
        var provider = new FakeProvider(() => Json(HttpStatusCode.OK, ForecastBody));
        var now = new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero);

        var result = await Create(provider, now).GetWeatherAsync("Kandy", new DateTime(2026, 10, 19));

        Assert.Equal("Available", result.Status);
    }

    [Fact]
    public async Task ProviderOutOfRangeResponse_BecomesUnavailableDateAdvisory()
    {
        var provider = new FakeProvider(() => Json(HttpStatusCode.BadRequest, ProviderOutOfRangeBody));

        // Inside our window, so the provider is asked and it rejects the date.
        var result = await Create(provider).GetWeatherAsync("Kandy", new DateTime(2026, 10, 10));

        Assert.Equal("Unavailable_DateOutOfRange", result.Status);
        Assert.Equal("Forecast is not yet available for this date.", result.Advisory);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task ValidForecast_WithZeroPrecipitation_KeepsZero()
    {
        var provider = new FakeProvider(() => Json(HttpStatusCode.OK, ForecastBody));

        var result = await Create(provider).GetWeatherAsync("Kandy", new DateTime(2026, 10, 10));

        Assert.Equal("Available", result.Status);
        Assert.Equal(29.5, result.MaxTemperatureC);
        Assert.Equal(21.0, result.MinTemperatureC);
        Assert.Equal(0.0, result.PrecipitationSumMm);
        Assert.Equal("Generally clear conditions are forecast.", result.Advisory);
    }

    [Fact]
    public async Task ProviderServerError_StaysAdvisory_AndHidesProviderText()
    {
        var provider = new FakeProvider(() => Json(HttpStatusCode.InternalServerError, "secret provider failure text"));

        var result = await Create(provider).GetWeatherAsync("Kandy", new DateTime(2026, 10, 10));

        Assert.Equal("Unavailable_ProviderError", result.Status);
        Assert.DoesNotContain("secret", result.Advisory);
        Assert.DoesNotContain("http", result.Advisory, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NetworkFailure_StaysAdvisory_AndHidesExceptionMessage()
    {
        var provider = new FakeProvider(() => throw new HttpRequestException("connect failed to api.open-meteo.com"));

        var result = await Create(provider).GetWeatherAsync("Kandy", new DateTime(2026, 10, 10));

        Assert.Equal("Unavailable_ProviderError", result.Status);
        Assert.DoesNotContain("open-meteo", result.Advisory, StringComparison.OrdinalIgnoreCase);
    }
}
