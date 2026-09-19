namespace TourManagement.Api.Dtos.Transport;

// What the search endpoint returns for each available vehicle.
public class VehicleSearchResultDto
{
    public int VehicleId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PricePerDay { get; set; }
}

