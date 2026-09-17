namespace TourManagement.Api.Models;

// Represents a hotel registered by a HotelOwner.
// A hotel must be approved by Admin before travelers can see or book it.
public class Hotel
{
    public int Id { get; set; }

    // The HotelOwner user who created this listing.
    public int OwnerId { get; set; }

    // Which destination this hotel is located at.
    public int DestinationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Sri Lankan phone: 0XXXXXXXXX or +94XXXXXXXXX (validated on the DTO).
    public string ContactPhone { get; set; } = string.Empty;

    // Optional 1-5 star rating. Nullable — owner may not know it yet.
    public int? StarRating { get; set; }

    // Optional single cover photo URL. Just a URL for now — no file upload logic.
    public string? ImageUrl { get; set; }

    // Stored as a string in the DB (see AppDbContext).
    public HotelStatus Status { get; set; } = HotelStatus.PendingApproval;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — EF Core uses these to join related tables.
    public User Owner { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
    public List<Room> Rooms { get; set; } = new();
}
