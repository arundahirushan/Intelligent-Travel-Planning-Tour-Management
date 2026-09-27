using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Profile;
using TourManagement.Api.Models;

namespace TourManagement.Api.Profile;

public class SupplierDeletionGuard : IAccountDeletionGuard
{
    private readonly AppDbContext _db;

    public SupplierDeletionGuard(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DeletionEligibilityDto> CheckAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;

        var blockingCount = await _db.SupplyOrders
            .Where(o => o.Supply.SupplierId == userId
                     && (o.Status == BookingStatus.Held || o.Status == BookingStatus.Confirmed)
                     && o.Trip.EndDate >= today)
            .CountAsync();

        return new DeletionEligibilityDto
        {
            CanDelete = blockingCount == 0,
            BlockingCount = blockingCount,
            BlockingMessage = blockingCount > 0
                ? $"You have {blockingCount} active or upcoming supply order(s). These must be completed or cancelled before deleting your account."
                : null
        };
    }
}
