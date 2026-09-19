using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert HotelBooking entities to DTOs.
public static class HotelBookingMappings
{
    // Assumes booking.Room and booking.Room.Hotel are loaded.
    // TotalPrice is calculated here — it's not stored in the DB.
    public static HotelBookingSummaryDto ToSummaryDto(this HotelBooking booking)
    {
        int nights = (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days;

        return new HotelBookingSummaryDto
        {
            Id            = booking.Id,
            HotelName     = booking.Room?.Hotel?.Name ?? string.Empty,
            RoomType      = booking.Room?.RoomType ?? string.Empty,
            CheckInDate   = booking.CheckInDate,
            CheckOutDate  = booking.CheckOutDate,
            NumberOfRooms = booking.NumberOfRooms,
            Status        = booking.Status,
            // PricePerNight × number of nights × number of rooms booked.
            TotalPrice    = booking.Room != null
                ? booking.Room.PricePerNight * nights * booking.NumberOfRooms
                : 0m
        };
    }
}
