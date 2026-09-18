using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Transport;

// Used in booking list responses. TotalPrice is calculated, not stored.
public class VehicleBookingSummaryDto
{
    public int Id { get; set; }

    // Vehicle details — included so the provider knows which physical vehicle.
    public string VehicleType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Where the vehicle should be delivered.
    public decimal PickupLatitude { get; set; }
    public decimal PickupLongitude { get; set; }
    public string? PickupNote { get; set; }

    public VehicleBookingStatus Status { get; set; }

    // PricePerDay × number of days. Calculated in mapping, not stored in the DB.
    public decimal TotalPrice { get; set; }
}

