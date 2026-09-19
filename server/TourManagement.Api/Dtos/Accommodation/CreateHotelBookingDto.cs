using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Accommodation;

// What the client sends when creating a new hotel booking.
public class CreateHotelBookingDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "NumberOfRooms must be at least 1.")]
    public int NumberOfRooms { get; set; }
}
