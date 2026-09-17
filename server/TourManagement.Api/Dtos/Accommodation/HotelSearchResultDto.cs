namespace TourManagement.Api.Dtos.Accommodation;

// One row in the hotel search results.
// Each row is one available room type at one hotel.
public class HotelSearchResultDto
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public int? StarRating { get; set; }
    public int RoomId { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }

    // TotalRooms minus already-booked rooms for the requested dates.
    public int AvailableRoomCount { get; set; }
}
