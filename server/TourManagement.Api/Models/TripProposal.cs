using System.ComponentModel.DataAnnotations.Schema;

namespace TourManagement.Api.Models;

public class TripProposal
{
    public int Id { get; set; }
    public string ProposalId { get; set; } = string.Empty; // Backend-generated unique ID
    public int TripId { get; set; }
    public int Version { get; set; }
    
    // Generation request identifier for retry handling
    public string RequestId { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public string InputSnapshot { get; set; } = "{}";
    
    [Column(TypeName = "jsonb")]
    public string Payload { get; set; } = "{}";
    
    public ProposalStatus Status { get; set; } = ProposalStatus.Generating;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Traveler/Admin decisions
    public string? TravelerDecision { get; set; }
    public DateTime? TravelerDecisionAt { get; set; }
    
    public int? AdminId { get; set; }
    public string? AdminDecision { get; set; }
    public DateTime? AdminDecisionAt { get; set; }
    public string? FailureReason { get; set; }
    
    // Checkout reference when a hold is placed
    public int? CheckoutId { get; set; }
    
    // Relationships
    public Trip Trip { get; set; } = null!;
    public TripCheckout? Checkout { get; set; }
    public User? Admin { get; set; }
    
    public List<ExecutionSummary> ExecutionSummaries { get; set; } = new();
}
