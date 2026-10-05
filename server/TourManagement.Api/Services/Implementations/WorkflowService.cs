using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TourManagement.Api.AgentIntegration;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Dtos.Workflows;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

public class WorkflowService : IWorkflowService
{
    private readonly AppDbContext _db;
    private readonly IAgentServiceClient _agentClient;
    private readonly ICheckoutService _checkoutService;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(AppDbContext db, IAgentServiceClient agentClient, ICheckoutService checkoutService, ILogger<WorkflowService> logger)
    {
        _db = db;
        _agentClient = agentClient;
        _checkoutService = checkoutService;
        _logger = logger;
    }

    public async Task<TripProposalDto> GenerateProposalAsync(int tripId, int travelerId)
    {
        var trip = await _db.Trips
            .Include(t => t.ItineraryItems)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null) throw new NotFoundException("Trip not found");
        if (trip.TravelerId != travelerId) throw new ForbiddenException("Not your trip");

        // Simple idempotency check (don't generate if already generating).
        // A Generating proposal that is past the stale limit was abandoned, so it no longer blocks a new attempt.
        var existingGenerating = await _db.TripProposals
            .Where(p => p.TripId == tripId && p.Status == ProposalStatus.Generating)
            .FirstOrDefaultAsync();

        if (existingGenerating != null && !await FailIfStaleAsync(existingGenerating))
        {
            throw new ValidationException("A proposal is already being generated for this trip.");
        }

        var nextVersion = await _db.TripProposals.Where(p => p.TripId == tripId).CountAsync() + 1;
        var proposalId = Guid.NewGuid().ToString();
        var requestId = Guid.NewGuid().ToString();

        var inputSnapshot = new
        {
            TripId = trip.Id,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Budget = trip.Budget,
            GroupSize = trip.GroupSize,
            Interests = trip.Interests,
            PickupLatitude = trip.PickupLatitude,
            PickupLongitude = trip.PickupLongitude,
            PickupNote = trip.PickupNote,
            Destinations = trip.ItineraryItems.Select(i => new { i.DestinationId, i.DayNumber }).ToList()
        };

        var inputSnapshotJson = JsonSerializer.Serialize(inputSnapshot);

        var proposal = new TripProposal
        {
            ProposalId = proposalId,
            TripId = tripId,
            Version = nextVersion,
            RequestId = requestId,
            InputSnapshot = inputSnapshotJson,
            Payload = "{}",
            Status = ProposalStatus.Generating,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TripProposals.Add(proposal);
        await _db.SaveChangesAsync();

        try
        {
            var result = await _agentClient.GenerateProposalAsync(proposalId, tripId, inputSnapshotJson);

            proposal.Status = Enum.Parse<ProposalStatus>(result.Status);
            proposal.Payload = result.Payload;
            proposal.UpdatedAt = DateTime.UtcNow;

            if (result.ExecutionSummaries != null)
            {
                foreach (var es in result.ExecutionSummaries)
                {
                    proposal.ExecutionSummaries.Add(new ExecutionSummary
                    {
                        AgentIdentity = es.AgentIdentity,
                        Status = es.Status,
                        ToolName = es.ToolName,
                        ResultSummary = es.ResultSummary,
                        ValidationResults = es.ValidationResults,
                        Errors = es.Errors,
                        RetryCount = es.RetryCount,
                        FinalOutcome = es.FinalOutcome,
                        StartedAt = DateTime.SpecifyKind(es.StartedAt, DateTimeKind.Utc),
                        CompletedAt = es.CompletedAt.HasValue ? DateTime.SpecifyKind(es.CompletedAt.Value, DateTimeKind.Utc) : null
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during AI proposal generation for TripId {TripId}, ProposalId {ProposalId}: {ExceptionType} - {Message}", tripId, proposalId, ex.GetType().Name, ex.Message);
            proposal.Status = ProposalStatus.GenerationFailed;
            proposal.FailureReason = "AI Service unavailable or generation timed out.";
            proposal.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // The agents finished but the database rejected the result (e.g. a value that is not valid jsonb).
            // Never leave the proposal on Generating: record a terminal failure instead.
            _logger.LogError(ex, "Could not save AI result for TripId {TripId}, ProposalId {ProposalId}: {ExceptionType}", tripId, proposalId, ex.GetType().Name);
            _db.ChangeTracker.Clear();
            proposal = await _db.TripProposals.FirstAsync(p => p.ProposalId == proposalId);
            proposal.Status = ProposalStatus.GenerationFailed;
            proposal.FailureReason = "The AI result could not be saved. Please try again.";
            proposal.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return await ToDtoWithDetailsAsync(proposal);
    }

    // Only the IDs we need from the saved payload. Extra fields are ignored.
    private record PayloadRoomRef(int RoomId);
    private record PayloadVehicleRef(int VehicleId);
    private record PayloadRefs(List<PayloadRoomRef>? Hotels, PayloadVehicleRef? Vehicle);

    /// <summary>
    /// Builds the normal proposal DTO and adds display-only hotel/room/vehicle details looked up from the
    /// IDs saved in the payload. The saved payload itself is never changed, and a missing or deleted
    /// room/vehicle is simply left out (the UI shows "Details unavailable").
    /// </summary>
    private async Task<TripProposalDto> ToDtoWithDetailsAsync(TripProposal proposal)
    {
        var dto = proposal.ToResponseDto();

        PayloadRefs? refs;
        try
        {
            refs = JsonSerializer.Deserialize<PayloadRefs>(proposal.Payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return dto; // Unreadable payload: show it as before, without extra details.
        }
        if (refs == null) return dto;

        var details = new ProposalDisplayDetailsDto();

        var roomIds = (refs.Hotels ?? new()).Select(h => h.RoomId).Distinct().ToList();
        if (roomIds.Count > 0)
        {
            // One query for all rooms, with their hotel and destination names.
            details.Rooms = await _db.Rooms
                .AsNoTracking()
                .Where(r => roomIds.Contains(r.Id))
                .Select(r => new ProposalRoomDetailDto
                {
                    RoomId = r.Id,
                    HotelId = r.HotelId,
                    HotelName = r.Hotel.Name,
                    DestinationName = r.Hotel.Destination.Name,
                    RoomType = r.RoomType,
                    Capacity = r.Capacity
                })
                .ToListAsync();
        }

        if (refs.Vehicle != null)
        {
            details.Vehicle = await _db.Vehicles
                .AsNoTracking()
                .Where(v => v.Id == refs.Vehicle.VehicleId)
                .Select(v => new ProposalVehicleDetailDto
                {
                    VehicleId = v.Id,
                    VehicleType = v.VehicleType,
                    Model = v.Model,
                    Capacity = v.Capacity
                })
                .FirstOrDefaultAsync();
        }

        dto.DisplayDetails = details;
        return dto;
    }

    /// <summary>
    /// Moves a proposal that has been Generating longer than <see cref="AgentTimeouts.GeneratingStaleAfter"/> to
    /// GenerationFailed. That limit is above the agent request timeout, so a generation that is still
    /// legitimately running is never touched. Returns true when the proposal was marked failed.
    /// </summary>
    private async Task<bool> FailIfStaleAsync(TripProposal proposal)
    {
        if (proposal.Status != ProposalStatus.Generating ||
            proposal.CreatedAt >= DateTime.UtcNow - AgentTimeouts.GeneratingStaleAfter)
        {
            return false;
        }

        proposal.Status = ProposalStatus.GenerationFailed;
        proposal.FailureReason = "Generation did not finish in time and was stopped. Please try again.";
        proposal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TripProposalDto> GetLatestProposalAsync(int tripId, int travelerId)
    {
        var trip = await _db.Trips.FindAsync(tripId);
        if (trip == null) throw new NotFoundException("Trip not found");
        if (trip.TravelerId != travelerId) throw new ForbiddenException("Not your trip");

        var proposal = await _db.TripProposals
            .Include(p => p.ExecutionSummaries)
            .Where(p => p.TripId == tripId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();

        if (proposal == null) throw new NotFoundException("No proposal found");

        await FailIfStaleAsync(proposal);

        return await ToDtoWithDetailsAsync(proposal);
    }

    public async Task<TripProposalDto> GetProposalByIdAsync(int tripId, string proposalId, int travelerId)
    {
        var proposal = await _db.TripProposals
            .Include(p => p.ExecutionSummaries)
            .Include(p => p.Trip)
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId && p.TripId == tripId);

        if (proposal == null) throw new NotFoundException("Proposal not found");
        if (proposal.Trip.TravelerId != travelerId) throw new ForbiddenException("Not your trip");

        await FailIfStaleAsync(proposal);

        return await ToDtoWithDetailsAsync(proposal);
    }

    public async Task<TripProposalDto> AcceptProposalAsync(int tripId, string proposalId, int travelerId)
    {
        var proposal = await _db.TripProposals
            .Include(p => p.Trip)
                .ThenInclude(t => t.ItineraryItems)
            .Include(p => p.ExecutionSummaries)
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId && p.TripId == tripId);

        if (proposal == null) throw new NotFoundException("Proposal not found");
        if (proposal.Trip.TravelerId != travelerId) throw new ForbiddenException("Not your trip");

        if (proposal.Status != ProposalStatus.Generated)
            throw new ValidationException("Only Generated proposals can be accepted.");

        var m4Pass = proposal.ExecutionSummaries.Any(es => es.AgentIdentity == "m4_validation" && es.FinalOutcome == "Pass");
        if (!m4Pass)
        {
            throw new ValidationException("Proposal must explicitly pass M4 validation before it can be accepted.");
        }

        CreateCheckoutDto checkoutDto;
        try
        {
            checkoutDto = JsonSerializer.Deserialize<CreateCheckoutDto>(proposal.Payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("Deserialization returned null");
        }
        catch
        {
            throw new ValidationException("Failed to read proposal payload.");
        }
        await _checkoutService.ValidateAgenticProposalAsync(checkoutDto, proposal.Trip);

        proposal.Status = ProposalStatus.PendingAdminApproval;
        proposal.TravelerDecision = "Accept";
        proposal.TravelerDecisionAt = DateTime.UtcNow;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await ToDtoWithDetailsAsync(proposal);
    }

    public async Task<TripProposalDto> RejectProposalAsync(int tripId, string proposalId, int travelerId, string? reason)
    {
        var proposal = await _db.TripProposals
            .Include(p => p.Trip)
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId && p.TripId == tripId);

        if (proposal == null) throw new NotFoundException("Proposal not found");
        if (proposal.Trip.TravelerId != travelerId) throw new ForbiddenException("Not your trip");

        if (proposal.Status != ProposalStatus.Generated && proposal.Status != ProposalStatus.PendingAdminApproval)
            throw new ValidationException("Cannot reject this proposal.");

        proposal.Status = ProposalStatus.TravelerRejected;
        proposal.TravelerDecision = "Reject";
        proposal.TravelerDecisionAt = DateTime.UtcNow;
        proposal.FailureReason = reason;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await ToDtoWithDetailsAsync(proposal);
    }

    public async Task<PagedResult<TripProposalDto>> GetPendingProposalsAsync(int page, int pageSize)
    {
        var query = _db.TripProposals
            .Include(p => p.ExecutionSummaries)
            .Where(p => p.Status == ProposalStatus.PendingAdminApproval)
            .OrderBy(p => p.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<TripProposalDto>
        {
            Items = items.Select(i => i.ToResponseDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TripProposalDto> GetAdminProposalByIdAsync(string proposalId)
    {
        var proposal = await _db.TripProposals
            .Include(p => p.ExecutionSummaries)
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId);

        if (proposal == null) throw new NotFoundException("Proposal not found");
        return proposal.ToResponseDto();
    }

    public async Task<TripProposalDto> ApproveProposalAsync(string proposalId, int adminId)
    {
        var proposal = await _db.TripProposals
            .Include(p => p.Trip)
                .ThenInclude(t => t.ItineraryItems)
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId);

        if (proposal == null) throw new NotFoundException("Proposal not found");
        if (proposal.Status != ProposalStatus.PendingAdminApproval)
            throw new ValidationException("Proposal is not pending admin approval.");

        // We must map the proposal payload to CreateCheckoutDto
        // Ensure payload is valid JSON and has hotels/vehicle.
        CreateCheckoutDto checkoutDto;
        try
        {
            checkoutDto = JsonSerializer.Deserialize<CreateCheckoutDto>(proposal.Payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("Deserialization returned null");
        }
        catch
        {
            throw new ValidationException("Failed to read proposal payload.");
        }

        checkoutDto.TripId = proposal.TripId;
        checkoutDto.ProposalId = proposal.ProposalId;

        await _checkoutService.ValidateAgenticProposalAsync(checkoutDto, proposal.Trip);

        if (checkoutDto.Hotels.Count == 0 && checkoutDto.Hotel == null && checkoutDto.Vehicle == null)
        {
            proposal.Status = ProposalStatus.ApprovedNoBookingRequired;
            proposal.AdminId = adminId;
            proposal.AdminDecision = "Approve";
            proposal.AdminDecisionAt = DateTime.UtcNow;
            proposal.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return proposal.ToResponseDto();
        }

        // Needs hold
        var checkout = await _checkoutService.PlaceApprovedProposalHoldAsync(checkoutDto, proposal.Trip.TravelerId);

        proposal.Status = ProposalStatus.HoldPlaced;
        proposal.CheckoutId = checkout.Id;
        proposal.AdminId = adminId;
        proposal.AdminDecision = "Approve";
        proposal.AdminDecisionAt = DateTime.UtcNow;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return proposal.ToResponseDto();
    }

    public async Task<TripProposalDto> RejectProposalAdminAsync(string proposalId, int adminId, string? reason)
    {
        var proposal = await _db.TripProposals.FirstOrDefaultAsync(p => p.ProposalId == proposalId);
        if (proposal == null) throw new NotFoundException("Proposal not found");
        if (proposal.Status != ProposalStatus.PendingAdminApproval)
            throw new ValidationException("Proposal is not pending admin approval.");

        proposal.Status = ProposalStatus.AdminRejected;
        proposal.AdminId = adminId;
        proposal.AdminDecision = "Reject";
        proposal.AdminDecisionAt = DateTime.UtcNow;
        proposal.FailureReason = reason;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return proposal.ToResponseDto();
    }
}
