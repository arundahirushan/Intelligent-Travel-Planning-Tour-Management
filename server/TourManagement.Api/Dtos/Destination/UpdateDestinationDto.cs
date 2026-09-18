using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Destination;

// What the client sends when updating an existing destination.
// Same fields as Create — all are updatable.
public class UpdateDestinationDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Region { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
}
