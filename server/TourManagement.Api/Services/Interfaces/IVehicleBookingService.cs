using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Transport;

namespace TourManagement.Api.Services.Interfaces;

public interface IVehicleBookingService
{
    // ── Traveler operations ───────────────────────────────────────────────────

    Task<VehicleBookingSummaryDto> CreateAsync(CreateVehicleBookingDto dto, int travelerId);
    Task<PagedResult<VehicleBookingSummaryDto>> GetMyBookingsAsync(int travelerId, int page, int pageSize);
    Task CancelAsync(int bookingId, int requestingUserId, string requestingUserRole);

    // ── Admin / SuperAdmin operations ────────────────────────────────────────

    Task<PagedResult<VehicleBookingSummaryDto>> GetAllBookingsAsync(string? status, int page, int pageSize);
}

