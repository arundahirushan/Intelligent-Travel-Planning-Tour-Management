using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using TourManagement.Api.Configurations;
using TourManagement.Api.Services.Interfaces;
using TourManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/payhere")]
public class PayHereController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly PayHereSettings _payHereSettings;
    private readonly AppDbContext _db;
    private readonly ILogger<PayHereController> _logger;

    public PayHereController(ICheckoutService checkoutService, PayHereSettings payHereSettings, AppDbContext db, ILogger<PayHereController> logger)
    {
        _checkoutService = checkoutService;
        _payHereSettings = payHereSettings;
        _db = db;
        _logger = logger;
    }

    [HttpPost("notify")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Notify([FromForm] IFormCollection form)
    {
        try
        {
            var merchantId = form["merchant_id"];
            var orderId = form["order_id"];
            var payhereAmount = form["payhere_amount"];
            var payhereCurrency = form["payhere_currency"];
            var statusCode = form["status_code"];
            var md5sig = form["md5sig"];
            var custom1 = form["custom_1"];
            var custom2 = form["custom_2"];
            
            var merchantSecretHash = GetMd5Hash(_payHereSettings.MerchantSecret).ToUpper();
            var localMd5sig = GetMd5Hash(merchantId + orderId + payhereAmount + payhereCurrency + statusCode + merchantSecretHash).ToUpper();

            if (localMd5sig != md5sig)
            {
                _logger.LogWarning("Invalid PayHere signature for order {OrderId}", orderId.ToString());
                return BadRequest("Invalid signature");
            }

            if (int.TryParse(orderId, out int checkoutId))
            {
                var attempt = await _db.PaymentAttempts
                    .OrderByDescending(pa => pa.CreatedAt)
                    .FirstOrDefaultAsync(pa => pa.TripCheckoutId == checkoutId);
                    
                if (attempt != null)
                {
                    attempt.Status = statusCode == "2" ? "Success" : "Failed";
                    attempt.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                if (statusCode == "2") // Success
                {
                    try
                    {
                        await _checkoutService.ConfirmAsync(checkoutId);
                    }
                    catch (TourManagement.Api.Common.Exceptions.ValidationException ex)
                    {
                        _logger.LogWarning(ex, "Payment received for order {OrderId} but confirmation failed (likely expired/cancelled).", orderId);
                        // We still return Ok() to PayHere because payment was recorded, but bookings are not confirmed.
                    }
                    catch (TourManagement.Api.Common.Exceptions.NotFoundException ex)
                    {
                        _logger.LogWarning(ex, "Payment received for unknown order {OrderId}.", orderId);
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayHere notification");
            return StatusCode(500);
        }
    }

    private static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
