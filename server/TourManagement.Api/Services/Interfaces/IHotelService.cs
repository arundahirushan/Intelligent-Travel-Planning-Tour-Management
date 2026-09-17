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

    Task<HotelDetailDto> AddRoomAsync(int hotelId, CreateRoomDto dto, int requestingUserId);
    Task<HotelDetailDto> UpdateRoomAsync(int hotelId, int roomId, UpdateRoomDto dto, int requestingUserId);
    Task<HotelDetailDto> DeactivateRoomAsync(int hotelId, int roomId, int requestingUserId);

    Task<PagedResult<BookingSummaryDto>> GetHotelBookingsAsync(int hotelId, int requestingUserId, int page, int pageSize);

    // ── Shared helper ─────────────────────────────────────────────────────────

    // Counts rooms already booked (Held or Confirmed) for a room type in a date range.
    // Used by both SearchAsync and BookingService to avoid duplicating the overlap logic.
    Task<int> CountBookedRoomsAsync(int roomId, DateTime checkIn, DateTime checkOut);

    // ── Admin / SuperAdmin operations ────────────────────────────────────────

    Task<PagedResult<HotelSummaryDto>> GetAllHotelsAsync(string? status, int? destinationId, int? ownerId, string? search, string? sort, int page, int pageSize);
    Task<PagedResult<HotelSummaryDto>> GetPendingHotelsAsync(int page, int pageSize);
    Task ApproveHotelAsync(int hotelId);
    Task SuspendHotelAsync(int hotelId);  // used for both reject and policy suspension

    Task<PagedResult<BookingSummaryDto>> GetAllBookingsAsync(string? status, int page, int pageSize);

    // ── Public / Traveler operations ─────────────────────────────────────────

    Task<List<HotelSearchResultDto>> SearchAsync(HotelSearchRequestDto request);
    Task<HotelDetailDto> GetPublicHotelByIdAsync(int hotelId, int requestingUserId, string requestingUserRole);
}
