using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Accommodation;

public class RoomWithHotelDto
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
    public int TotalRooms { get; set; }
    public string? Amenities { get; set; }
    public RoomStatus Status { get; set; }
}
