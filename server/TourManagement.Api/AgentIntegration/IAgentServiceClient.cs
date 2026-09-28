namespace TourManagement.Api.AgentIntegration;

public class AgentProposalResult
{
    public string Status { get; set; } = string.Empty; // e.g. "Generated", "GenerationFailed", "NeedsInput"
    public string Payload { get; set; } = "{}"; // JSON string
    public List<AgentExecutionSummary> ExecutionSummaries { get; set; } = new();
}

public class AgentExecutionSummary
{
    public string AgentIdentity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ToolName { get; set; }
    public string? ResultSummary { get; set; } // JSON string
    public string? ValidationResults { get; set; } // JSON string
    public string? Errors { get; set; } // JSON string
    public int RetryCount { get; set; }
    public string FinalOutcome { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public interface IAgentServiceClient
{
    // Requests the AI service to generate a proposal for the given trip input.
    Task<AgentProposalResult> GenerateProposalAsync(string proposalId, int tripId, string requestSnapshotJson);
}
