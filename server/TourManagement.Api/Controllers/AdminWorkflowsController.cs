using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Workflows;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/admin/workflows")]
[Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin)]
public class AdminWorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public AdminWorkflowsController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<PagedResult<TripProposalDto>>>> GetPending(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _workflowService.GetPendingProposalsAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<TripProposalDto>>.Ok(result));
    }

    [HttpGet("{proposalId}")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> GetById([FromRoute] string proposalId)
    {
        var proposal = await _workflowService.GetAdminProposalByIdAsync(proposalId);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal));
    }

    [HttpPost("{proposalId}/approve")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> Approve([FromRoute] string proposalId)
    {
        var adminId = int.Parse(User.FindFirst("id")!.Value);
        var proposal = await _workflowService.ApproveProposalAsync(proposalId, adminId);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal, "Proposal approved and hold placed."));
    }

    [HttpPost("{proposalId}/reject")]
    public async Task<ActionResult<ApiResponse<TripProposalDto>>> Reject([FromRoute] string proposalId, [FromBody] WorkflowDecisionDto decision)
    {
        var adminId = int.Parse(User.FindFirst("id")!.Value);
        var proposal = await _workflowService.RejectProposalAdminAsync(proposalId, adminId, decision.Reason);
        return Ok(ApiResponse<TripProposalDto>.Ok(proposal, "Proposal rejected by admin."));
    }
}
