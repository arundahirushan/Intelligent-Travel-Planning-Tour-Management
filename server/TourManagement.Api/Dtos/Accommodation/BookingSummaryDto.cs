using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Accommodation;

// What the API returns for a booking (in list views and detail).
public class BookingSummaryDto
{
    public int Id { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfRooms { get; set; }
    public BookingStatus Status { get; set; }

    // Computed: PricePerNight × nights × NumberOfRooms.
    // Not stored in the DB — calculated in the mapping method.
    public decimal TotalPrice { get; set; }
}
