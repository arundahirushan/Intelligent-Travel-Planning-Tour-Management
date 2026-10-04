namespace TourManagement.Api.Configurations;

public class PayHereSettings
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantSecret { get; set; } = string.Empty;
    public bool IsSandbox { get; set; } = true;
    public string NotifyUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
