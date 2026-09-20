using TourManagement.Api.Common;
using TourManagement.Api.Dtos.SupplyOrders;

namespace TourManagement.Api.Services.Interfaces;

public interface ISupplyOrderService
{
    Task<SupplyOrderSummaryDto> CreateAsync(CreateSupplyOrderDto dto, int travelerId);
    Task<SupplyOrderSummaryDto> UpdateAsync(int id, UpdateSupplyOrderDto dto, int travelerId);
    Task DeleteAsync(int id, int requestingUserId, string requestingUserRole);
    
    Task<PagedResult<SupplyOrderSummaryDto>> GetMyOrdersAsync(
        int travelerId, string? status, string? sort, int page, int pageSize);
        
    Task<SupplyOrderSummaryDto> CancelAsync(int id, int userId);
    
    Task<PagedResult<SupplyOrderSummaryDto>> GetReceivedOrdersAsync(
        int supplierId, string? status, string? sort, int page, int pageSize);
        
    Task<PagedResult<SupplyOrderSummaryDto>> GetAllAdminAsync(
        string? status, string? sort, int page, int pageSize);
}
