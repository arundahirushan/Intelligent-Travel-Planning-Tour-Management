using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Accommodation;

// Same fields as Create — all hotel details are updatable.
public class UpdateHotelDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int DestinationId { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0\d{9}|\+94\d{9})$",
        ErrorMessage = "ContactPhone must be in the format 0XXXXXXXXX or +94XXXXXXXXX.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "StarRating must be between 1 and 5.")]
    public int? StarRating { get; set; }

    public string? ImageUrl { get; set; }
}
