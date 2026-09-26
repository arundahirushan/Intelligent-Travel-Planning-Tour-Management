using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelsController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found in token.");
        return int.Parse(claim);
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("Role claim not found in token.");
    }

    // ── HotelOwner endpoints ─────────────────────────────────────────────────

    /// <summary>Create a new hotel listing. Starts as PendingApproval. HotelOwner only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<HotelDetailDto>>> Create([FromBody] CreateHotelDto dto)
    {
        var result = await _hotelService.CreateHotelAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<HotelDetailDto>.Ok(result, "Hotel created. Pending Admin approval."));
    }

    /// <summary>Get the owner's own hotels with optional search, sort, and pagination. HotelOwner only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<PagedResult<HotelSummaryDto>>>> GetMyHotels(
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _hotelService.GetMyHotelsAsync(GetCurrentUserId(), search, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<HotelSummaryDto>>.Ok(result));
    }

    /// <summary>Get full detail of one of the owner's own hotels. HotelOwner only.</summary>
    [HttpGet("my/{id}")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<HotelDetailDto>>> GetMyHotelById(int id)
    {
        var result = await _hotelService.GetMyHotelByIdAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<HotelDetailDto>.Ok(result));
    }

    /// <summary>Get all rooms across all hotels owned by the owner. HotelOwner only.</summary>
    [HttpGet("my/rooms")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoomWithHotelDto>>>> GetMyRooms(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _hotelService.GetMyRoomsAsync(GetCurrentUserId(), search, status, page, pageSize);
        return Ok(ApiResponse<PagedResult<RoomWithHotelDto>>.Ok(result));
    }

    /// <summary>Update hotel details. HotelOwner only (ownership checked in service).</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<HotelDetailDto>>> Update(int id, [FromBody] UpdateHotelDto dto)
    {
        var result = await _hotelService.UpdateHotelAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<HotelDetailDto>.Ok(result, "Hotel updated."));
    }

    /// <summary>Soft-delete a hotel (sets Status = Inactive). HotelOwner only.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _hotelService.DeactivateHotelAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok("Hotel deactivated."));
    }

    /// <summary>Restore an inactive hotel (sets Status = PendingApproval). HotelOwner only.</summary>
    [HttpPatch("{id}/restore")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse>> Restore(int id)
    {
        await _hotelService.RestoreHotelAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok("Hotel restored to Pending status."));
    }

    /// <summary>Add a room type to the hotel. HotelOwner only.</summary>
    [HttpPost("{hotelId}/rooms")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<HotelDetailDto>>> AddRoom(int hotelId, [FromBody] CreateRoomDto dto)
    {
        var result = await _hotelService.AddRoomAsync(hotelId, dto, GetCurrentUserId());
        return Ok(ApiResponse<HotelDetailDto>.Ok(result, "Room added."));
    }

    /// <summary>Update a room type. HotelOwner only.</summary>
    [HttpPut("{hotelId}/rooms/{roomId}")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<HotelDetailDto>>> UpdateRoom(
        int hotelId, int roomId, [FromBody] UpdateRoomDto dto)
    {
        var result = await _hotelService.UpdateRoomAsync(hotelId, roomId, dto, GetCurrentUserId());
        return Ok(ApiResponse<HotelDetailDto>.Ok(result, "Room updated."));
    }

    /// <summary>Soft-delete a room type (sets Status = Inactive). HotelOwner only.</summary>
    [HttpDelete("{hotelId}/rooms/{roomId}")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<HotelDetailDto>>> DeleteRoom(int hotelId, int roomId)
    {
        var result = await _hotelService.DeactivateRoomAsync(hotelId, roomId, GetCurrentUserId());
        return Ok(ApiResponse<HotelDetailDto>.Ok(result, "Room deactivated."));
    }

    /// <summary>Get bookings for this hotel. HotelOwner only (ownership checked in service).</summary>
    [HttpGet("{hotelId}/bookings")]
    [Authorize(Roles = Roles.HotelOwner)]
    public async Task<ActionResult<ApiResponse<PagedResult<HotelBookingSummaryDto>>>> GetHotelBookings(
        int hotelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _hotelService.GetHotelBookingsAsync(hotelId, GetCurrentUserId(), page, pageSize);
        return Ok(ApiResponse<PagedResult<HotelBookingSummaryDto>>.Ok(result));
    }

    // ── Admin / SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>List ALL hotels with filters. Admin / SuperAdmin only.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<HotelSummaryDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? destinationId,
        [FromQuery] int? ownerId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _hotelService.GetAllHotelsAsync(
            status, destinationId, ownerId, search, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<HotelSummaryDto>>.Ok(result));
    }

    /// <summary>List hotels awaiting approval. Admin / SuperAdmin only.</summary>
    [HttpGet("pending")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<HotelSummaryDto>>>> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _hotelService.GetPendingHotelsAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<HotelSummaryDto>>.Ok(result));
    }

    /// <summary>Approve a hotel (Status → Active). Admin / SuperAdmin only.</summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Approve(int id)
    {
        await _hotelService.ApproveHotelAsync(id);
        return Ok(ApiResponse.Ok("Hotel approved."));
    }

    /// <summary>
    /// Reject a pending hotel listing (Status → Rejected). Admin / SuperAdmin only.
    /// Only valid when the hotel is still PendingApproval.
    /// To disable an already-Active hotel, use the suspend endpoint instead.
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Reject(int id)
    {
        await _hotelService.RejectHotelAsync(id);
        return Ok(ApiResponse.Ok("Hotel listing rejected."));
    }

    /// <summary>Suspend an active hotel (Status → Suspended). Admin / SuperAdmin only. Use reject for pending listings.</summary>
    [HttpPost("{id}/suspend")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Suspend(int id)
    {
        await _hotelService.SuspendHotelAsync(id);
        return Ok(ApiResponse.Ok("Hotel suspended."));
    }

    /// <summary>All bookings across all hotels. Admin / SuperAdmin only.</summary>
    [HttpGet("bookings-all")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<HotelBookingSummaryDto>>>> GetAllBookings(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _hotelService.GetAllBookingsAsync(status, page, pageSize);
        return Ok(ApiResponse<PagedResult<HotelBookingSummaryDto>>.Ok(result));
    }

    // ── Public / Traveler endpoints ──────────────────────────────────────────

    /// <summary>Get all accepted hotels. Publicly accessible.</summary>
    [HttpGet("accepted")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<HotelSummaryDto>>>> GetAcceptedHotels(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _hotelService.GetAllHotelsAsync("Accepted", null, null, null, null, page, pageSize);
        return Ok(ApiResponse<PagedResult<HotelSummaryDto>>.Ok(result));
    }

    /// <summary>Search available hotels by destination, dates, guests, and budget. Any authenticated user.</summary>
    [HttpGet("search")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<HotelSearchResultDto>>>> Search(
        [FromQuery] int destinationId,
        [FromQuery] DateTime checkInDate,
        [FromQuery] DateTime checkOutDate,
        [FromQuery] decimal? maxBudgetPerNight,
        [FromQuery] int numberOfGuests = 1)
    {
        var request = new HotelSearchRequestDto
        {
            DestinationId      = destinationId,
            CheckInDate        = checkInDate,
            CheckOutDate       = checkOutDate,
            MaxBudgetPerNight  = maxBudgetPerNight,
            NumberOfGuests     = numberOfGuests
        };

        var result = await _hotelService.SearchAsync(request);
        return Ok(ApiResponse<List<HotelSearchResultDto>>.Ok(result));
    }

    /// <summary>Get hotel detail. Active hotels visible to all; owner and admins see any status.</summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<HotelDetailDto>>> GetById(int id)
    {
        var result = await _hotelService.GetPublicHotelByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse<HotelDetailDto>.Ok(result));
    }
}
