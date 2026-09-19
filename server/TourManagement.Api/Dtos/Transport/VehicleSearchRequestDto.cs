using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Transport;

// Input for the vehicle search endpoint (GET /api/vehicles/search).
public class VehicleSearchRequestDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    // If provided, only vehicles with Capacity >= MinCapacity are returned.
    public int? MinCapacity { get; set; }

    // If provided, only vehicles at or below this price per day are returned.
    public decimal? MaxPricePerDay { get; set; }
}

