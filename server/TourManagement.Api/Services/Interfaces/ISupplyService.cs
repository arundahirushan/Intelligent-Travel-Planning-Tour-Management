using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Supplier;

namespace TourManagement.Api.Services.Interfaces;

public interface ISupplyService
{
    // Supplier role operations
    Task<SupplyDetailDto> CreateAsync(CreateSupplyDto dto, int supplierId);
    Task<PagedResult<SupplySummaryDto>> GetMySuppliesAsync(
        int supplierId, string? search, string? category, string? status, string? sort, int page, int pageSize);
    Task<SupplyDetailDto> GetMySupplyByIdAsync(int id, int supplierId);
    Task<SupplyDetailDto> UpdateAsync(int id, UpdateSupplyDto dto, int supplierId);
    Task DeactivateAsync(int id, int supplierId);
    Task<SupplyDetailDto> RepublishAsync(int id, int supplierId);

    // Admin role operations
    Task<PagedResult<SupplySummaryDto>> GetAllAdminAsync(
        string? search, string? category, string? status, int? supplierId, string? sort, int page, int pageSize);
    Task<SupplyDetailDto> RemoveAsync(int id, RemoveSupplyDto dto);

    // Public / Authenticated browse operations
    Task<PagedResult<SupplySummaryDto>> BrowseActiveAsync(
        string? search, string? category, string? sort, int page, int pageSize);
}
