using TourManagement.Api.Dtos.SupplyOrders;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

public static class SupplyOrderMappings
{
    public static SupplyOrderSummaryDto ToSummaryDto(this SupplyOrder order)
    {
        return new SupplyOrderSummaryDto
        {
            Id = order.Id,
            SupplyName = order.Supply?.Name ?? string.Empty,
            Quantity = order.Quantity,
            PriceAtOrderTime = order.PriceAtOrderTime,
            TotalPrice = order.Quantity * order.PriceAtOrderTime,
            Status = order.Status
        };
    }
}
