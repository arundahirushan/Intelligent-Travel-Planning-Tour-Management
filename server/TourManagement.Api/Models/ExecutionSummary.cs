using System.ComponentModel.DataAnnotations.Schema;

namespace TourManagement.Api.Models;

public class ExecutionSummary
{
    public int Id { get; set; }
    public int TripProposalId { get; set; }
    
    public string AgentIdentity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ToolName { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? ResultSummary { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? ValidationResults { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? Errors { get; set; }
    
    public int RetryCount { get; set; }
    public string FinalOutcome { get; set; } = string.Empty;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Relationships
    public TripProposal TripProposal { get; set; } = null!;
}
