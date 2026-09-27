using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Accommodation;

// Full hotel details including its room types. Used for the detail view.
public class HotelDetailDto
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int DestinationId { get; set; }
    public string DestinationName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public int? StarRating { get; set; }
    public string? ImageUrl { get; set; }
    public HotelStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int OccupancyPercentage { get; set; }

    // All room types belonging to this hotel.
    public List<RoomDto> Rooms { get; set; } = new();
}
