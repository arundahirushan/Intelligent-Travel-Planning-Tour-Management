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

    // Descriptive hotel/room/vehicle data looked up from the IDs in Payload. Display only.
    public ProposalDisplayDetailsDto? DisplayDetails { get; set; }
    
    public string? TravelerDecision { get; set; }
    public string? AdminDecision { get; set; }
    public string? FailureReason { get; set; }
    
    public List<ExecutionSummaryDto> ExecutionSummaries { get; set; } = new();
}

// Display-only lookups for the IDs saved in a proposal payload.
// A room or vehicle that no longer exists is simply left out; the UI shows "Details unavailable".
public class ProposalDisplayDetailsDto
{
    public List<ProposalRoomDetailDto> Rooms { get; set; } = new();
    public ProposalVehicleDetailDto? Vehicle { get; set; }
}

public class ProposalRoomDetailDto
{
    public int RoomId { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    // Maximum guests per room (Room.Capacity).
    public int Capacity { get; set; }
}

public class ProposalVehicleDetailDto
{
    public int VehicleId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    // Vehicle.Model holds "make and model" as one value; there is no separate make field.
    public string Model { get; set; } = string.Empty;
    // Maximum passengers (Vehicle.Capacity).
    public int Capacity { get; set; }
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
