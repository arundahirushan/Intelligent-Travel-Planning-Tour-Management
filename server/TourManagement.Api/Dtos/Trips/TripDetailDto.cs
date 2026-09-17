using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Trips;

// Full trip detail returned by GET /api/trips/{id}.
// Includes all trip fields plus the complete itinerary.
public class TripDetailDto
{
    public int Id { get; set; }
    public int TravelerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public int GroupSize { get; set; }
    public string? Interests { get; set; }
    public TripStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Itinerary items ordered by DayNumber then SequenceOrder (done in the service).
    public List<ItineraryItemDto> ItineraryItems { get; set; } = new();
}
