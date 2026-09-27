using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Profile;
using TourManagement.Api.Models;

namespace TourManagement.Api.Profile;

public class TransportProviderDeletionGuard : IAccountDeletionGuard
{
    private readonly AppDbContext _db;

    public TransportProviderDeletionGuard(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DeletionEligibilityDto> CheckAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;

        var blockingCount = await _db.VehicleBookings
            .Where(b => b.Vehicle.ProviderId == userId
                     && (b.Status == BookingStatus.Held || b.Status == BookingStatus.Confirmed)
                     && b.EndDate >= today)
            .CountAsync();

        return new DeletionEligibilityDto
        {
            CanDelete = blockingCount == 0,
            BlockingCount = blockingCount,
            BlockingMessage = blockingCount > 0
                ? $"You have {blockingCount} active or upcoming vehicle booking(s). These must be completed or cancelled before deleting your account."
                : null
        };
    }
}
