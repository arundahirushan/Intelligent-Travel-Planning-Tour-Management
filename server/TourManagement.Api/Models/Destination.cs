namespace TourManagement.Api.Models;

// A travel destination that can be included in trip itineraries.
// Managed by Admin/SuperAdmin only.
public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Optional URL to an image of this destination. Nullable — not required.
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property — links to all itinerary items that reference this destination.
    // Deleting a Destination that has itinerary items is BLOCKED at the DB level (Restrict),
    // so this list exists mainly for EF Core to set up the relationship correctly.
    public List<ItineraryItem> ItineraryItems { get; set; } = new();
}
