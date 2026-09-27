using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Accommodation;

namespace TourManagement.Api.Services.Interfaces;

public interface IHotelService
{
    // ── HotelOwner operations ────────────────────────────────────────────────

    Task<HotelDetailDto> CreateHotelAsync(CreateHotelDto dto, int ownerId);
    Task<PagedResult<HotelSummaryDto>> GetMyHotelsAsync(int ownerId, string? search, string? sort, int page, int pageSize);
    Task<HotelDetailDto> GetMyHotelByIdAsync(int hotelId, int ownerId);
    Task<HotelDetailDto> UpdateHotelAsync(int hotelId, UpdateHotelDto dto, int requestingUserId);
    Task DeactivateHotelAsync(int hotelId, int requestingUserId);  // sets Status = Inactive
    Task RestoreHotelAsync(int hotelId, int requestingUserId);

    Task<PagedResult<RoomWithHotelDto>> GetMyRoomsAsync(int ownerId, string? search, string? status, int page, int pageSize);

    Task<HotelDetailDto> AddRoomAsync(int hotelId, CreateRoomDto dto, int requestingUserId);
    Task<HotelDetailDto> UpdateRoomAsync(int hotelId, int roomId, UpdateRoomDto dto, int requestingUserId);
    Task<HotelDetailDto> DeactivateRoomAsync(int hotelId, int roomId, int requestingUserId);

    Task<PagedResult<HotelBookingSummaryDto>> GetHotelBookingsAsync(int hotelId, int requestingUserId, int page, int pageSize);

    // ── Shared helper ──────────────────────────────────────────────────

    // Counts rooms already booked (Held or Confirmed) for a room type in a date range.
    // Used by both SearchAsync and HotelBookingService to avoid duplicating the overlap logic.
    Task<int> CountBookedRoomsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);

    // Computes the occupancy percentage (0-100) for a given hotel on a specific date.
    Task<int> ComputeOccupancyAsync(int hotelId, DateTime today);

    // ── Admin / SuperAdmin operations ────────────────────────────────────────

    Task<PagedResult<HotelSummaryDto>> GetAllHotelsAsync(string? status, int? destinationId, int? ownerId, string? search, string? sort, int page, int pageSize);
    Task<PagedResult<HotelSummaryDto>> GetPendingHotelsAsync(int page, int pageSize);
    Task ApproveHotelAsync(int hotelId);
    Task RejectHotelAsync(int hotelId);   // only valid from PendingApproval → Rejected
    Task SuspendHotelAsync(int hotelId);  // only valid from Active → Suspended

    Task<PagedResult<HotelBookingSummaryDto>> GetAllBookingsAsync(string? status, int page, int pageSize);

    // ── Public / Traveler operations ─────────────────────────────────────────

    Task<List<HotelSearchResultDto>> SearchAsync(HotelSearchRequestDto request);
    Task<HotelDetailDto> GetPublicHotelByIdAsync(int hotelId, int requestingUserId, string requestingUserRole);
}
