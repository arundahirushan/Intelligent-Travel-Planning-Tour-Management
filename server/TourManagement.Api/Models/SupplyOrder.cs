namespace TourManagement.Api.Models;

public class SupplyOrder
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public int SupplyId { get; set; }

    public int Quantity { get; set; }

    // Price per unit captured at the exact moment of order.
    // Preserves history if the supplier changes the price later.
    public decimal PriceAtOrderTime { get; set; }

    // Reuse existing BookingStatus enum
    public BookingStatus Status { get; set; } = BookingStatus.Held;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public Supply Supply { get; set; } = null!;
}
