using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to map TripCheckout and its linked booking items to response DTOs.
public static class CheckoutMappings
{
    // Maps the checkout entity to the base response DTO (without item details).
    // Item details are filled in by CheckoutService.LoadCheckoutDtoAsync after
    // loading the related bookings separately.
    public static CheckoutResponseDto ToResponseDto(this TripCheckout checkout)
    {
        return new CheckoutResponseDto
        {
            Id            = checkout.Id,
            TripId        = checkout.TripId,
            Status        = checkout.Status,
            HoldExpiresAt = checkout.HoldExpiresAt,
            TotalPrice    = checkout.TotalPrice,
            WebsiteFee    = checkout.WebsiteFee,
            CreatedAt     = checkout.CreatedAt,
        };
    }

    // Maps a HotelBooking to a HotelHoldItemDto, using the price captured at hold time.
    // Assumes booking.Room and booking.Room.Hotel are loaded.
    public static HotelHoldItemDto ToHoldItemDto(this HotelBooking booking, decimal priceSnapshot)
    {
        return new HotelHoldItemDto
        {
            HotelBookingId = booking.Id,
            HotelName      = booking.Room?.Hotel?.Name ?? string.Empty,
            RoomType       = booking.Room?.RoomType    ?? string.Empty,
            CheckInDate    = booking.CheckInDate,
            CheckOutDate   = booking.CheckOutDate,
            NumberOfRooms  = booking.NumberOfRooms,
            PriceSnapshot  = priceSnapshot,
        };
    }

    // Maps a VehicleBooking to a VehicleHoldItemDto, using the price captured at hold time.
    // Assumes booking.Vehicle is loaded.
    public static VehicleHoldItemDto ToHoldItemDto(this VehicleBooking booking, decimal priceSnapshot)
    {
        return new VehicleHoldItemDto
        {
            VehicleBookingId   = booking.Id,
            VehicleModel       = booking.Vehicle?.Model              ?? string.Empty,
            VehicleType        = booking.Vehicle?.VehicleType        ?? string.Empty,
            RegistrationNumber = booking.Vehicle?.RegistrationNumber ?? string.Empty,
            StartDate          = booking.StartDate,
            EndDate            = booking.EndDate,
            PriceSnapshot      = priceSnapshot,
        };
    }

    // Maps a SupplyOrder to a SupplyHoldItemDto, using the price captured at hold time.
    // Assumes order.Supply is loaded.
    public static SupplyHoldItemDto ToHoldItemDto(this SupplyOrder order)
    {
        return new SupplyHoldItemDto
        {
            SupplyOrderId = order.Id,
            SupplyName    = order.Supply?.Name        ?? string.Empty,
            Description   = order.Supply?.Description ?? string.Empty,
            Quantity      = order.Quantity,
            PriceSnapshot = order.PriceAtOrderTime,
        };
    }
}
