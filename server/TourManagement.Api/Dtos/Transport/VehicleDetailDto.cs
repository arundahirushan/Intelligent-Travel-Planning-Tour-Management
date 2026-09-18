using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Transport;

// Full vehicle details. Used for the detail view and create/update responses.
public class VehicleDetailDto
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PricePerDay { get; set; }
    public VehicleStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

