using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Accommodation;

// What the API returns for a single room type.
public class RoomDto
{
    public int Id { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
    public int TotalRooms { get; set; }
    public string? Amenities { get; set; }
    public RoomStatus Status { get; set; }
}
