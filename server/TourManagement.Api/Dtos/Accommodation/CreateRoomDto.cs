using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Accommodation;

// What the client sends when adding a new room type to a hotel.
public class CreateRoomDto
{
    [Required]
    public string RoomType { get; set; } = string.Empty;

    // Price in LKR. Must be greater than zero.
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerNight must be greater than 0.")]
    public decimal PricePerNight { get; set; }

    // Max guests this room type can fit.
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }

    // How many physical rooms of this type exist in the hotel.
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "TotalRooms must be at least 1.")]
    public int TotalRooms { get; set; }

    // Comma-separated tags, e.g. "AC,WiFi,Breakfast". Optional.
    public string? Amenities { get; set; }
}
