namespace TourManagement.Api.Dtos.Workflows;

public class GenerateProposalRequestDto
{
    // The Trip to generate a proposal for.
    public int TripId { get; set; }
}

public class WorkflowDecisionDto
{
    // E.g. "Accept", "Reject", "Regenerate" for Traveler
    // "Approve", "Reject" for Admin
    public string Decision { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class TripProposalDto
{
    public int Id { get; set; }
    public string ProposalId { get; set; } = string.Empty;
    public int TripId { get; set; }
    public int Version { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Deserialized jsonb
    public object? InputSnapshot { get; set; }
    public object? Payload { get; set; }
    
    public string? TravelerDecision { get; set; }
    public string? AdminDecision { get; set; }
    public string? FailureReason { get; set; }
    
    public List<ExecutionSummaryDto> ExecutionSummaries { get; set; } = new();
}

public class ExecutionSummaryDto
{
    public int Id { get; set; }
    public string AgentIdentity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ToolName { get; set; }
    public object? ResultSummary { get; set; }
    public object? ValidationResults { get; set; }
    public object? Errors { get; set; }
    public int RetryCount { get; set; }
    public string FinalOutcome { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
