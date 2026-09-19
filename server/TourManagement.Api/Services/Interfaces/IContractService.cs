using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Supplier;

namespace TourManagement.Api.Services.Interfaces;

public interface IContractService
{
    Task<ContractDetailDto> CreateFirstContractAsync(CreateContractDto dto);
    Task<PagedResult<ContractSummaryDto>> GetAllContractsAsync(
        string? computedStatus, int? supplierId, string? sort, int page, int pageSize);
    Task<ContractDetailDto> GetByIdAsync(int id);
    Task<ContractDetailDto> TerminateAsync(int id);

    // Business-specific operation: checks if the supplier currently has an active, unexpired contract.
    // Reusable by other services (e.g. SupplyOrder).
    Task<bool> IsContractCurrentlyValidAsync(int supplierId);

    // Formatted contract status summary for dashboard views.
    Task<SupplierContractStatusDto> GetContractStatusSummaryAsync(int supplierId);
}
