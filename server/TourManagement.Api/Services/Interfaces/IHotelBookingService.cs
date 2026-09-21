using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Accommodation;

namespace TourManagement.Api.Services.Interfaces;

public interface IHotelBookingService
{
    // Traveler creates a booking (starts as Held).
    Task<HotelBookingSummaryDto> CreateAsync(CreateHotelBookingDto dto, int travelerId);

    // Traveler views their own bookings (joined through Trip).
    Task<PagedResult<HotelBookingSummaryDto>> GetMyBookingsAsync(int travelerId, string? status, int page, int pageSize);

    // Traveler or Admin cancels a booking.
    Task CancelAsync(int bookingId, int requestingUserId, string requestingUserRole);

    Task<HotelBookingSummaryDto> UpdateAsync(int id, UpdateHotelBookingDto dto, int travelerId);
    Task DeleteAsync(int id, int requestingUserId, string requestingUserRole);
}
