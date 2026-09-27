using System.Text.Json;
using TourManagement.Api.Dtos.Workflows;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

public static class WorkflowMappings
{
    public static TripProposalDto ToResponseDto(this TripProposal entity)
    {
        return new TripProposalDto
        {
            Id = entity.Id,
            ProposalId = entity.ProposalId,
            TripId = entity.TripId,
            Version = entity.Version,
            RequestId = entity.RequestId,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            InputSnapshot = !string.IsNullOrEmpty(entity.InputSnapshot) 
                ? JsonSerializer.Deserialize<object>(entity.InputSnapshot) : null,
            Payload = !string.IsNullOrEmpty(entity.Payload) 
                ? JsonSerializer.Deserialize<object>(entity.Payload) : null,
            TravelerDecision = entity.TravelerDecision,
            AdminDecision = entity.AdminDecision,
            FailureReason = entity.FailureReason,
            ExecutionSummaries = entity.ExecutionSummaries?.Select(e => e.ToResponseDto()).ToList() ?? new List<ExecutionSummaryDto>()
        };
    }

    public static ExecutionSummaryDto ToResponseDto(this ExecutionSummary entity)
    {
        return new ExecutionSummaryDto
        {
            Id = entity.Id,
            AgentIdentity = entity.AgentIdentity,
            Status = entity.Status,
            ToolName = entity.ToolName,
            ResultSummary = !string.IsNullOrEmpty(entity.ResultSummary) ? JsonSerializer.Deserialize<object>(entity.ResultSummary) : null,
            ValidationResults = !string.IsNullOrEmpty(entity.ValidationResults) ? JsonSerializer.Deserialize<object>(entity.ValidationResults) : null,
            Errors = !string.IsNullOrEmpty(entity.Errors) ? JsonSerializer.Deserialize<object>(entity.Errors) : null,
            RetryCount = entity.RetryCount,
            FinalOutcome = entity.FinalOutcome,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt
        };
    }
}
