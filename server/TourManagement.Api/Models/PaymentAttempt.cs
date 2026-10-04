namespace TourManagement.Api.Models;

public class PaymentAttempt
{
    public int Id { get; set; }
    public int TripCheckoutId { get; set; }
    public string? PayHerePaymentId { get; set; }
    
    // Amount we requested from PayHere (always 1000 for now)
    public decimal Amount { get; set; }
    
    // Status can be Pending, Success, Failed
    public string Status { get; set; } = "Pending";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public TripCheckout TripCheckout { get; set; } = null!;
}
