namespace TourManagement.Api.Models;

public enum ProposalStatus
{
    Generating,
    Generated,
    GenerationFailed,
    NeedsInput,          // Similar to NeedsRevision
    PendingAdminApproval,
    TravelerRejected,
    Superseded,
    AdminRejected,
    HoldPlaced,
    ApprovedNoBookingRequired
}
