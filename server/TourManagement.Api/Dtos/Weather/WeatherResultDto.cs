namespace TourManagement.Api.Dtos.Weather;

public class WeatherResultDto
{
    public string Destination { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Source { get; set; } = "Open-Meteo";
    public DateTime RetrievalTime { get; set; }
    
    // Status can be "Available", "Unavailable_DateOutOfRange", "Unavailable_LocationNotFound", "Unavailable_ProviderError"
    public string Status { get; set; } = string.Empty;
    
    // Advisory message for the agent/traveler
    public string Advisory { get; set; } = string.Empty;

    // Optional metrics
    public int? WeatherCode { get; set; }
    public double? MaxTemperatureC { get; set; }
    public double? MinTemperatureC { get; set; }
    public double? PrecipitationSumMm { get; set; }
}

