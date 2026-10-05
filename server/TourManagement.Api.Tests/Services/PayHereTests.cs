using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Services.Implementations;
using TourManagement.Api.Models;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Configurations;
using Xunit;

namespace TourManagement.Api.Tests.Services;

public class PayHerePaymentTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CheckoutService _checkoutService;
    private readonly PayHereSettings _settings;

    public PayHerePaymentTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        
        var hotelService = new HotelService(_db);
        var vehicleService = new VehicleService(_db);
        _settings = new PayHereSettings { MerchantId = "TEST", MerchantSecret = "SECRET", IsSandbox = true };
        
        _checkoutService = new CheckoutService(_db, hotelService, vehicleService, null!, _settings);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task<TripCheckout> CreateTestCheckout(CheckoutStatus status, DateTime holdExpiresAt)
    {
        var traveler = new User { Id = 1, Role = "Traveler", FullName = "Test", Email = "test@example.com" };
        var trip = new Trip { Id = 1, TravelerId = 1, Title = "T", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        _db.Users.Add(traveler);
        _db.Trips.Add(trip);

        var checkout = new TripCheckout
        {
            TripId = trip.Id,
            TravelerId = traveler.Id,
            WebsiteFee = 1000m,
            TotalPrice = 1000m,
            Status = status,
            HoldExpiresAt = holdExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.TripCheckouts.Add(checkout);
        await _db.SaveChangesAsync();
        return checkout;
    }

    [Fact]
    public async Task ConfirmAsync_WithActiveCheckout_ConfirmsBookings()
    {
        var checkout = await CreateTestCheckout(CheckoutStatus.Active, DateTime.UtcNow.AddHours(1));
        
        await _checkoutService.ConfirmAsync(checkout.Id);

        var updatedCheckout = await _db.TripCheckouts.FindAsync(checkout.Id);
        Assert.Equal(CheckoutStatus.Paid, updatedCheckout!.Status);
        
        var updatedTrip = await _db.Trips.FindAsync(checkout.TripId);
        Assert.Equal(TripStatus.Confirmed, updatedTrip!.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WithExpiredHold_ThrowsValidationException()
    {
        var checkout = await CreateTestCheckout(CheckoutStatus.Active, DateTime.UtcNow.AddHours(-1));
        
        await Assert.ThrowsAsync<ValidationException>(() => _checkoutService.ConfirmAsync(checkout.Id));
    }

    [Fact]
    public async Task ConfirmAsync_WithPaidCheckout_ThrowsValidationException()
    {
        var checkout = await CreateTestCheckout(CheckoutStatus.Paid, DateTime.UtcNow.AddHours(1));
        
        await Assert.ThrowsAsync<ValidationException>(() => _checkoutService.ConfirmAsync(checkout.Id));
    }
}
