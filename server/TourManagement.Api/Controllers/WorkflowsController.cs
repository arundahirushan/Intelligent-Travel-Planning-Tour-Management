using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Workflows;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/workflows")]
[Authorize(Roles = Roles.Traveler)]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowsController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> Generate([FromRoute] int tripId)
    {
        var travelerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var proposal = await _workflowService.GenerateProposalAsync(tripId, travelerId);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal, "Generation started/completed."));
    }

    [HttpGet("proposal")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> GetLatest([FromRoute] int tripId)
    {
        var travelerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var proposal = await _workflowService.GetLatestProposalAsync(tripId, travelerId);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal));
    }

    [HttpGet("{proposalId}")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> GetById([FromRoute] int tripId, [FromRoute] string proposalId)
    {
        var travelerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var proposal = await _workflowService.GetProposalByIdAsync(tripId, proposalId, travelerId);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal));
    }

    [HttpPost("{proposalId}/accept")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> Accept([FromRoute] int tripId, [FromRoute] string proposalId)
    {
        var travelerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var proposal = await _workflowService.AcceptProposalAsync(tripId, proposalId, travelerId);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal, "Proposal accepted and pending admin approval."));
    }

    [HttpPost("{proposalId}/reject")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> Reject([FromRoute] int tripId, [FromRoute] string proposalId, [FromBody] WorkflowDecisionDto decision)
    {
        var travelerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var proposal = await _workflowService.RejectProposalAsync(tripId, proposalId, travelerId, decision.Reason);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal, "Proposal rejected."));
    }
}
