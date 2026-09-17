using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Trips;

// What the client sends when creating a new trip.
public class CreateTripDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    // Budget must be greater than zero — further validated in the service layer.
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Budget must be greater than 0.")]
    public decimal Budget { get; set; }

    // At least 1 person in the group.
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "GroupSize must be at least 1.")]
    public int GroupSize { get; set; }

    // Optional comma-separated interests, e.g. "hiking,beach,food".
    public string? Interests { get; set; }
}
