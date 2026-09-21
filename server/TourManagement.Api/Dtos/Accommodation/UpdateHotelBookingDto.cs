using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Accommodation;

public class UpdateHotelBookingDto
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "You must book at least 1 room.")]
    public int NumberOfRooms { get; set; }
}
