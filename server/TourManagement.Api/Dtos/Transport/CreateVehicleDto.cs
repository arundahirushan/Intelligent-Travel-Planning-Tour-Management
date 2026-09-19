using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Transport;

// What the client sends when a TransportProvider creates a new vehicle listing.
public class CreateVehicleDto
{
    [Required]
    public string VehicleType { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Required]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerDay must be greater than 0.")]
    public decimal PricePerDay { get; set; }
}

