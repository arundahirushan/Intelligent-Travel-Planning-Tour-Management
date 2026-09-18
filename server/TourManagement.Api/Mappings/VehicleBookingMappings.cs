using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert VehicleBooking entities to DTOs.
public static class VehicleBookingMappings
{
    // Assumes booking.Vehicle is loaded.
    // TotalPrice is calculated here — it's not stored in the DB.
    public static VehicleBookingSummaryDto ToSummaryDto(this VehicleBooking booking)
    {
        int days = (booking.EndDate.Date - booking.StartDate.Date).Days;

        return new VehicleBookingSummaryDto
        {
            Id                 = booking.Id,
            VehicleType        = booking.Vehicle?.VehicleType ?? string.Empty,
            Model              = booking.Vehicle?.Model ?? string.Empty,
            RegistrationNumber = booking.Vehicle?.RegistrationNumber ?? string.Empty,
            StartDate          = booking.StartDate,
            EndDate            = booking.EndDate,
            PickupLatitude     = booking.PickupLatitude,
            PickupLongitude    = booking.PickupLongitude,
            PickupNote         = booking.PickupNote,
            Status             = booking.Status,
            // PricePerDay × number of days.
            TotalPrice         = booking.Vehicle != null
                ? booking.Vehicle.PricePerDay * days
                : 0m
        };
    }
}

