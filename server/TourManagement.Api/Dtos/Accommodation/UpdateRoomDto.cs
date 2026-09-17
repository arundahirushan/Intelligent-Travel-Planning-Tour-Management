using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Accommodation;

// Same fields as Create — all room details are updatable.
public class UpdateRoomDto
{
    [Required]
    public string RoomType { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerNight must be greater than 0.")]
    public decimal PricePerNight { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "TotalRooms must be at least 1.")]
    public int TotalRooms { get; set; }

    public string? Amenities { get; set; }
}
