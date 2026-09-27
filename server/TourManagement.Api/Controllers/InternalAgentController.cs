using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Dtos.Trips;
using TourManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/internal")]
public class InternalAgentController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IHotelService _hotelService;
    private readonly IVehicleService _vehicleService;
    private readonly string _aiSecret;

    private readonly AppDbContext _db;

    public InternalAgentController(
        ITripService tripService, 
        IHotelService hotelService, 
        IVehicleService vehicleService, 
        AppDbContext db,
        IConfiguration config)
    {
        _tripService = tripService;
        _hotelService = hotelService;
        _vehicleService = vehicleService;
        _db = db;
        _aiSecret = config.GetValue<string>("AiServiceSettings:Secret") 
            ?? throw new InvalidOperationException("AiServiceSettings:Secret is not configured.");
    }

    private async Task<Models.TripProposal?> GetAuthorizedProposalAsync()
    {
        if (!Request.Headers.TryGetValue("X-AI-Secret", out var providedSecret) || providedSecret != _aiSecret)
            return null;

        if (!Request.Headers.TryGetValue("X-AI-ProposalId", out var proposalIdHeader))
            return null;

        var proposalId = proposalIdHeader.ToString();
        return await _db.TripProposals
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId && p.Status == Models.ProposalStatus.Generating);
    }

    [HttpGet("trips/current")]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> GetTrip()
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        var trip = await _tripService.GetByIdAsync(proposal.TripId, 0, "SuperAdmin");
        return Ok(ApiResponse<TripDetailDto>.Ok(trip));
    }

    [HttpPost("hotels/search")]
    public async Task<ActionResult<ApiResponse<List<HotelSearchResultDto>>>> SearchHotels([FromBody] HotelSearchRequestDto request)
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        // Enforce trip scope: hotels must be searched within the trip's destinations
        var trip = await _db.Trips.Include(t => t.ItineraryItems).FirstOrDefaultAsync(t => t.Id == proposal.TripId);
        if (trip == null || !trip.ItineraryItems.Any(i => i.DestinationId == request.DestinationId))
            return BadRequest(ApiResponse<List<HotelSearchResultDto>>.Fail("Requested destination is not part of the current trip."));

        var results = await _hotelService.SearchAsync(request);
        return Ok(ApiResponse<List<HotelSearchResultDto>>.Ok(results));
    }

    [HttpPost("vehicles/search")]
    public async Task<ActionResult<ApiResponse<List<VehicleSearchResultDto>>>> SearchVehicles([FromBody] VehicleSearchRequestDto request)
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        var results = await _vehicleService.SearchAsync(request);
        return Ok(ApiResponse<List<VehicleSearchResultDto>>.Ok(results));
    }
    
    [HttpGet("weather")]
    public async Task<ActionResult<ApiResponse<object>>> GetWeather([FromQuery] string destination, [FromQuery] DateTime date)
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        return StatusCode(501, ApiResponse<object>.Fail("Weather service is not implemented yet."));
    }
}
