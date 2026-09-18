using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Accommodation;

namespace TourManagement.Api.Services.Interfaces;

public interface IBookingService
{
    // Traveler creates a booking (starts as Held).
    Task<BookingSummaryDto> CreateAsync(CreateBookingDto dto, int travelerId);

    // Traveler views their own bookings (joined through Trip).
    Task<PagedResult<BookingSummaryDto>> GetMyBookingsAsync(int travelerId, string? status, int page, int pageSize);

    // Traveler or Admin cancels a booking.
    Task CancelAsync(int bookingId, int requestingUserId, string requestingUserRole);
}
