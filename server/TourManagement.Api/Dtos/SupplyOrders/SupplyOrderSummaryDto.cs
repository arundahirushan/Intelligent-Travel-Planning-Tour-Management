using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.SupplyOrders;

public class SupplyOrderSummaryDto
{
    public int Id { get; set; }
    
    public string SupplyName { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    public decimal PriceAtOrderTime { get; set; }
    
    // Computed value
    public decimal TotalPrice { get; set; }
    
    public BookingStatus Status { get; set; }
}
