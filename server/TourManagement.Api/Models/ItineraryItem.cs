namespace TourManagement.Api.Models;

// A single day/destination entry within a Trip's itinerary.
// The trip is divided into days, and each day can have one or more items
// (though generate-draft creates one item per day to start).
public class ItineraryItem
{
    public int Id { get; set; }

    // Foreign key — which trip this item belongs to.
    public int TripId { get; set; }

    // Foreign key — which destination the traveler visits on this day.
    public int DestinationId { get; set; }

    // Which day of the trip this is (1 = first day, 2 = second day, etc.).
    public int DayNumber { get; set; }

    // Free-text notes for this day (optional). Actual bookable activities
    // and transport are handled by separate team members' components.
    public string? Notes { get; set; }

    // For ordering multiple items within the same day.
    public int SequenceOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — used by EF Core to join related tables.
    public Trip Trip { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
}
