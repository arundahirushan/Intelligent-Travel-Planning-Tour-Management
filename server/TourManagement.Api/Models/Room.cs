namespace TourManagement.Api.Models;

// Represents a ROOM TYPE within a hotel — not one physical room.
// For example, "Standard Double" or "Deluxe Suite".
// TotalRooms tells us how many physical rooms of this type exist.
public class Room
{
    public int Id { get; set; }

    // Which hotel this room type belongs to.
    public int HotelId { get; set; }

    // Friendly name, e.g. "Standard Double", "Deluxe Suite".
    public string RoomType { get; set; } = string.Empty;

    // Price per night in LKR (this project is Sri Lanka only).
    public decimal PricePerNight { get; set; }

    // Maximum number of guests this room type can accommodate.
    public int Capacity { get; set; }

    // How many physical rooms of this type the hotel has.
    // Used to calculate availability during the search and booking validation.
    public int TotalRooms { get; set; }

    // Comma-separated amenity tags, e.g. "AC,WiFi,Breakfast".
    // Same approach as Trip.Interests — keeps it simple without a separate table.
    public string? Amenities { get; set; }

    // Stored as a string in the DB (see AppDbContext).
    public RoomStatus Status { get; set; } = RoomStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties.
    public Hotel Hotel { get; set; } = null!;
    public List<HotelBooking> Bookings { get; set; } = new();
}
