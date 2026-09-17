using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Destination;

// What the client sends when creating a new destination.
public class CreateDestinationDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    // Free-text region, e.g. "Southern Province". This project covers Sri Lanka only.
    [Required]
    public string Region { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    // Optional — not every destination needs a photo URL right away.
    public string? ImageUrl { get; set; }
}
