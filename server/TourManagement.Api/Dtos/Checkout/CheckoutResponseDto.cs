using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Checkout;

// What the API returns for a hotel hold item inside a CheckoutResponseDto.
public class HotelHoldItemDto
{
    public int HotelBookingId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfRooms { get; set; }

    // Price captured at the moment the hold was placed.
    // Later price changes on the room do NOT change this value.
    public decimal PriceSnapshot { get; set; }
}

// What the API returns for a vehicle hold item inside a CheckoutResponseDto.
public class VehicleHoldItemDto
{
    public int VehicleBookingId { get; set; }
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Price captured at the moment the hold was placed.
    public decimal PriceSnapshot { get; set; }
}

// What the API returns for a TripCheckout (hold session).
public class CheckoutResponseDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public CheckoutStatus Status { get; set; }

    // UTC timestamp. The frontend should display this in local time with a countdown.
    public DateTime HoldExpiresAt { get; set; }

    // Total price at hold time (sum of item snapshots).
    public decimal TotalPrice { get; set; }
    
    public decimal WebsiteFee { get; set; }

    // Either or both of these will be populated, depending on what was held.
    public HotelHoldItemDto? HotelItem { get; set; }
    public List<HotelHoldItemDto> Hotels { get; set; } = new();
    public VehicleHoldItemDto? VehicleItem { get; set; }
    public List<SupplyHoldItemDto> Supplies { get; set; } = new();

    public DateTime CreatedAt { get; set; }
}

public class SupplyHoldItemDto
{
    public int SupplyOrderId { get; set; }
    public string SupplyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceSnapshot { get; set; }
}
