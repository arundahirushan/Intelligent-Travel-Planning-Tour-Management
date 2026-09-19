using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Transport;

namespace TourManagement.Api.Services.Interfaces;

public interface IVehicleService
{
    // ── TransportProvider operations ─────────────────────────────────────────

    Task<VehicleDetailDto> CreateVehicleAsync(CreateVehicleDto dto, int providerId);
    Task<PagedResult<VehicleSummaryDto>> GetMyVehiclesAsync(int providerId, string? search, string? status, string? sort, int page, int pageSize);
    Task<VehicleDetailDto> GetMyVehicleByIdAsync(int vehicleId, int providerId);
    Task<VehicleDetailDto> UpdateVehicleAsync(int vehicleId, UpdateVehicleDto dto, int requestingUserId);
    Task DeactivateVehicleAsync(int vehicleId, int requestingUserId);  // sets Status = Inactive

    Task<PagedResult<VehicleBookingSummaryDto>> GetVehicleBookingsAsync(int vehicleId, int requestingUserId, int page, int pageSize);

    // ── Shared helper ─────────────────────────────────────────────────────────

    // Returns true if the vehicle is available (no Held or Confirmed booking overlaps the given dates).
    // Used by both SearchAsync and VehicleBookingService to avoid duplicating the overlap logic.
    Task<bool> IsVehicleAvailableAsync(int vehicleId, DateTime startDate, DateTime endDate);

    // ── Admin / SuperAdmin operations ────────────────────────────────────────

    Task<PagedResult<VehicleSummaryDto>> GetAllVehiclesAsync(string? status, int? providerId, string? search, string? sort, int page, int pageSize);
    Task<PagedResult<VehicleSummaryDto>> GetPendingVehiclesAsync(int page, int pageSize);
    Task ApproveVehicleAsync(int vehicleId);
    Task RejectVehicleAsync(int vehicleId);   // only valid from PendingApproval → Rejected
    Task SuspendVehicleAsync(int vehicleId);  // only valid from Active → Suspended

    // ── Public / Traveler operations ─────────────────────────────────────────

    Task<List<VehicleSearchResultDto>> SearchAsync(VehicleSearchRequestDto request);
    Task<VehicleDetailDto> GetPublicVehicleByIdAsync(int vehicleId, int requestingUserId, string requestingUserRole);
}

