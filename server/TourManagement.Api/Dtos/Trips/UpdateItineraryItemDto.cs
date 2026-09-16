using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Trips;

// What the client sends when editing an existing itinerary item.
// Same fields as Create.
public class UpdateItineraryItemDto
{
    [Required]
    public int DestinationId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DayNumber must be at least 1.")]
    public int DayNumber { get; set; }

    public string? Notes { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "SequenceOrder must be at least 1.")]
    public int SequenceOrder { get; set; }
}
