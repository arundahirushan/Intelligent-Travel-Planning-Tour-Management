namespace TourManagement.Api.Dtos.Trips;

// A single itinerary item as returned in responses.
// DestinationName is pulled from the related Destination — not stored
// redundantly in the ItineraryItem table.
public class ItineraryItemDto
{
    public int Id { get; set; }
    public int DestinationId { get; set; }
    public string DestinationName { get; set; } = string.Empty;  // joined from Destination
    public int DayNumber { get; set; }
    public string? Notes { get; set; }
    public int SequenceOrder { get; set; }
}
