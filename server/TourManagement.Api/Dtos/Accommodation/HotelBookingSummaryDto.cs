using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Accommodation;

// What the API returns for a hotel booking (in list views and detail).
public class HotelBookingSummaryDto
{
    public int Id { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfRooms { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime? HoldExpiresAt { get; set; }

    // This should now be the frozen price from TripCheckout, or the calculated fallback.
    public decimal TotalPrice { get; set; }
}
