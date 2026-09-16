using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Trips;

// What the client sends when manually adding an itinerary item to a trip.
public class CreateItineraryItemDto
{
    [Required]
    public int DestinationId { get; set; }

    // DayNumber must be >= 1. The service also checks it doesn't exceed the
    // total number of days in the trip.
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DayNumber must be at least 1.")]
    public int DayNumber { get; set; }

    // Optional free-text notes for this day.
    public string? Notes { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "SequenceOrder must be at least 1.")]
    public int SequenceOrder { get; set; }
}
