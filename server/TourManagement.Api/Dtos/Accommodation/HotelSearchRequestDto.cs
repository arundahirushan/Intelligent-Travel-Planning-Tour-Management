using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Accommodation;

// Input for the hotel search endpoint (GET /api/hotels/search).
public class HotelSearchRequestDto
{
    [Required]
    public int DestinationId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    // If provided, only rooms at or below this price per night are returned.
    public decimal? MaxBudgetPerNight { get; set; }

    // Only rooms whose Capacity >= NumberOfGuests are included.
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "NumberOfGuests must be at least 1.")]
    public int NumberOfGuests { get; set; }
}
