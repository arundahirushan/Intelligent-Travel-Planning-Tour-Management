using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Transport;

// What the client sends when a Traveler creates a new vehicle booking.
public class CreateVehicleBookingDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    // Coordinates where the traveler wants the vehicle delivered.
    // These come from a map picker on the frontend.
    [Required]
    [Range(-90.0, 90.0, ErrorMessage = "PickupLatitude must be between -90 and 90.")]
    public decimal PickupLatitude { get; set; }

    [Required]
    [Range(-180.0, 180.0, ErrorMessage = "PickupLongitude must be between -180 and 180.")]
    public decimal PickupLongitude { get; set; }

    // Optional delivery instructions, e.g. "gate code 1234".
    public string? PickupNote { get; set; }
}

