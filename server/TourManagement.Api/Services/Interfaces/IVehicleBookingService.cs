using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Transport;

namespace TourManagement.Api.Services.Interfaces;

public interface IVehicleBookingService
{
    // ── Traveler operations ───────────────────────────────────────────────────

    Task<VehicleBookingSummaryDto> CreateAsync(CreateVehicleBookingDto dto, int travelerId);
    Task<VehicleBookingSummaryDto> UpdateAsync(int id, UpdateVehicleBookingDto dto, int travelerId);
    Task DeleteAsync(int id, int requestingUserId, string requestingUserRole);
    Task<PagedResult<VehicleBookingSummaryDto>> GetMyBookingsAsync(int travelerId, int page, int pageSize);
    Task CancelAsync(int bookingId, int requestingUserId, string requestingUserRole);

    // Get bookings across all vehicles owned by a specific transport provider
    Task<PagedResult<VehicleBookingSummaryDto>> GetMyVehiclesBookingsAsync(int providerId, string? search, string? status, int page, int pageSize);

    // ── Admin / SuperAdmin operations ────────────────────────────────────────

    Task<PagedResult<VehicleBookingSummaryDto>> GetAllBookingsAsync(string? status, int page, int pageSize);
}
