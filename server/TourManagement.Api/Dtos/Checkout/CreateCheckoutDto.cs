using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Checkout;

// The hotel room selection the traveler wants to hold.
public class HotelCheckoutItemDto
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Required, Range(1, 100, ErrorMessage = "NumberOfRooms must be between 1 and 100.")]
    public int NumberOfRooms { get; set; }
}

// The vehicle selection the traveler wants to hold.
public class VehicleCheckoutItemDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required, Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal PickupLatitude { get; set; }

    [Required, Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal PickupLongitude { get; set; }

    public string? PickupNote { get; set; }
}

// Request body for POST /api/checkouts.
// At least one of Hotel or Vehicle must be non-null (validated in CheckoutService).
// The backend looks up prices itself — the traveler never sends a price.
public class CreateCheckoutDto
{
    [Required]
    public int TripId { get; set; }

    // Provide Hotel, Vehicle, or both.  At least one required.
    public HotelCheckoutItemDto? Hotel { get; set; }
    public List<HotelCheckoutItemDto> Hotels { get; set; } = new();
    public VehicleCheckoutItemDto? Vehicle { get; set; }
    
    // An identifier for the agentic proposal. 
    public string? ProposalId { get; set; }

    // Advisory weather data collected during proposal generation.
    public List<TourManagement.Api.Dtos.Weather.WeatherResultDto>? PartialWeather { get; set; }
}
