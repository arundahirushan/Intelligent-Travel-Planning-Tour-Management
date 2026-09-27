using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Workflows;

namespace TourManagement.Api.Services.Interfaces;

public interface IWorkflowService
{
    // Traveler
    Task<TripProposalDto> GenerateProposalAsync(int tripId, int travelerId);
    Task<TripProposalDto> GetLatestProposalAsync(int tripId, int travelerId);
    Task<TripProposalDto> GetProposalByIdAsync(int tripId, string proposalId, int travelerId);
    Task<TripProposalDto> AcceptProposalAsync(int tripId, string proposalId, int travelerId);
    Task<TripProposalDto> RejectProposalAsync(int tripId, string proposalId, int travelerId, string? reason);
    
    // Admin
    Task<PagedResult<TripProposalDto>> GetPendingProposalsAsync(int page, int pageSize);
    Task<TripProposalDto> GetAdminProposalByIdAsync(string proposalId);
    Task<TripProposalDto> ApproveProposalAsync(string proposalId, int adminId);
    Task<TripProposalDto> RejectProposalAdminAsync(string proposalId, int adminId, string? reason);
}
