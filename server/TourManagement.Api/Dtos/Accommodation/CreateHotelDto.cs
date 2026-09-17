using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Accommodation;

// What the client sends when a HotelOwner registers a new hotel.
public class CreateHotelDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int DestinationId { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    // Must be a valid Sri Lankan phone number: 0XXXXXXXXX or +94XXXXXXXXX.
    [Required]
    [RegularExpression(@"^(0\d{9}|\+94\d{9})$",
        ErrorMessage = "ContactPhone must be in the format 0XXXXXXXXX or +94XXXXXXXXX.")]
    public string ContactPhone { get; set; } = string.Empty;

    // Optional — owner may not know the star rating yet.
    [Range(1, 5, ErrorMessage = "StarRating must be between 1 and 5.")]
    public int? StarRating { get; set; }

    // Optional cover photo URL.
    public string? ImageUrl { get; set; }
}
