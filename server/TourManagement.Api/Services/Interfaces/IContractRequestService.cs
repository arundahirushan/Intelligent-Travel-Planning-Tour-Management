using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Supplier;

namespace TourManagement.Api.Services.Interfaces;

public interface IContractRequestService
{
    // Supplier role operations
    Task<ContractRequestSummaryDto> CreateRequestAsync(CreateContractRequestDto dto, int supplierId);
    Task<PagedResult<ContractRequestSummaryDto>> GetMyRequestsAsync(int supplierId, int page, int pageSize);

    // Common / Admin lookup
    Task<ContractRequestSummaryDto> GetByIdAsync(int id, int? requestingUserId, string? requestingUserRole);

    // Admin role operations
    Task<PagedResult<ContractRequestSummaryDto>> GetAllAdminAsync(string? status, int page, int pageSize);
    Task<ContractRequestSummaryDto> ApproveAsync(int id);
    Task<ContractRequestSummaryDto> RejectAsync(int id, RejectContractRequestDto dto);
}
