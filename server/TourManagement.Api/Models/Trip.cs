namespace TourManagement.Api.Models;

// Represents a trip planned by a Traveler.
public class Trip
{
    public int Id { get; set; }

    // Foreign key — which traveler owns this trip.
    public int TravelerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Budget in decimal — stored with 2 decimal places (see AppDbContext).
    public decimal Budget { get; set; }

    public int GroupSize { get; set; }

    // Simple comma-separated interest tags, e.g. "hiking,beach,food".
    // We keep this as a plain string for now — no separate tags table needed.
    public string? Interests { get; set; }

    // Stored as a string in the DB (see AppDbContext) for readability.
    public TripStatus Status { get; set; } = TripStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — EF Core uses these to join the related tables.
    public User Traveler { get; set; } = null!;
    public List<ItineraryItem> ItineraryItems { get; set; } = new();

    // Hotel bookings linked to this trip.
    public List<Booking> Bookings { get; set; } = new();

    // Vehicle bookings linked to this trip.
    public List<VehicleBooking> VehicleBookings { get; set; } = new();
}
