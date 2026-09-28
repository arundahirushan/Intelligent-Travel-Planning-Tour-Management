using TourManagement.Api.Dtos.Weather;

namespace TourManagement.Api.Services.Interfaces;

public interface IWeatherService
{
    Task<WeatherResultDto> GetWeatherAsync(string destinationName, DateTime date);
}

