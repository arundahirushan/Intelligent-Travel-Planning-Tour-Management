using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Trips;

// What the client sends when editing an existing trip.
// Same fields as Create — the service checks that the trip is still in Draft status.
public class UpdateTripDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Budget must be greater than 0.")]
    public decimal Budget { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "GroupSize must be at least 1.")]
    public int GroupSize { get; set; }

    public string? Interests { get; set; }
}
