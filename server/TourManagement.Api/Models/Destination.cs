namespace TourManagement.Api.Models;

// A travel destination that can be included in trip itineraries.
// Managed by Admin/SuperAdmin only.
public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Free-text region within Sri Lanka, e.g. "Southern Province", "Hill Country", "Colombo".
    public string Region { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Optional URL to an image of this destination. Nullable — not required.
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — EF Core uses these to join related tables.
    public List<ItineraryItem> ItineraryItems { get; set; } = new();
    public List<Hotel> Hotels { get; set; } = new();
}
