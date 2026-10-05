using System.Security.Cryptography;
using System.Text;

namespace TourManagement.Api.Dtos.Checkout;

public class PayHereInitiateResponseDto
{
    public string MerchantId { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Items { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string PayHereUrl { get; set; } = string.Empty;
}
