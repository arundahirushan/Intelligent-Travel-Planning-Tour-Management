using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Transport;

public class UpdateVehicleBookingDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal PickupLatitude { get; set; }

    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal PickupLongitude { get; set; }

    public string? PickupNote { get; set; }
}
