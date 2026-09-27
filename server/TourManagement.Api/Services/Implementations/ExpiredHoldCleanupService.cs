using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Models;

namespace TourManagement.Api.Services.Implementations;

// Background service that runs every 15 minutes and marks expired checkouts.
//
// IMPORTANT: This service is cosmetic only.  The availability checks in
// HotelService.CountBookedRoomsAsync and VehicleService.IsVehicleAvailableAsync
// already skip expired holds at query time — so availability is always correct
// regardless of whether this job has run.
//
// What this job does:
//   - Finds TripCheckouts where Status == Active AND HoldExpiresAt < UtcNow.
//   - Sets their Status to Expired.
//   - Sets any linked HotelBooking / VehicleBooking that are still Held to Cancelled.
//
// This makes the traveler's "My Checkouts" and Admin views show a clear terminal
// state (Expired / Cancelled) rather than stale Active records.
public class ExpiredHoldCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredHoldCleanupService> _logger;

    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(15);

    public ExpiredHoldCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredHoldCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpiredHoldCleanupService started.");

        // Wait briefly before first run so the app finishes starting up.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredHoldsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log but don't crash — the job will retry on the next interval.
                _logger.LogError(ex, "Error in ExpiredHoldCleanupService.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }

        _logger.LogInformation("ExpiredHoldCleanupService stopping.");
    }

    private async Task CleanupExpiredHoldsAsync(CancellationToken ct)
    {
        // Each cleanup run gets a fresh scoped DbContext (required for BackgroundService).
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Find all active checkouts past their expiry.
        var expiredCheckouts = await db.TripCheckouts
            .Where(c => c.Status == CheckoutStatus.Active && c.HoldExpiresAt <= now)
            .ToListAsync(ct);

        if (expiredCheckouts.Count == 0)
            return;

        _logger.LogInformation(
            "ExpiredHoldCleanupService: found {Count} expired checkouts to clean up.", expiredCheckouts.Count);

        foreach (var checkout in expiredCheckouts)
        {
            // Cancel the linked hotel booking if it is still Held.
            if (checkout.HotelBookingId.HasValue)
            {
                var hb = await db.HotelBookings.FindAsync(new object[] { checkout.HotelBookingId.Value }, ct);
                if (hb != null && hb.Status == BookingStatus.Held)
                {
                    hb.Status    = BookingStatus.Cancelled;
                    hb.UpdatedAt = now;
                }
            }

            // Cancel the linked vehicle booking if it is still Held.
            if (checkout.VehicleBookingId.HasValue)
            {
                var vb = await db.VehicleBookings.FindAsync(new object[] { checkout.VehicleBookingId.Value }, ct);
                if (vb != null && vb.Status == BookingStatus.Held)
                {
                    vb.Status    = BookingStatus.Cancelled;
                    vb.UpdatedAt = now;
                }
            }

            checkout.Status    = CheckoutStatus.Expired;
            checkout.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ExpiredHoldCleanupService: marked {Count} checkouts as Expired.", expiredCheckouts.Count);
    }
}
