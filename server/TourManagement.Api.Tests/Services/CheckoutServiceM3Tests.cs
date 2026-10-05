using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Services.Implementations;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Tests.Services;

public class CheckoutServiceM3Tests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CheckoutService _checkoutService;

    public CheckoutServiceM3Tests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);

        var hotelService = new HotelService(_db);
        var vehicleService = new VehicleService(_db);
        
        _checkoutService = new CheckoutService(_db, hotelService, vehicleService, null!, new TourManagement.Api.Configurations.PayHereSettings());
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task ValidateAgenticProposalAsync_SameDayTrip_CalculatesOneDayMinimum()
    {
        // Arrange
        var trip = new Trip
        {
            TravelerId = 10,
            Title = "Same Day Trip",
            StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), // Same day
            Budget = 100000,
            GroupSize = 2,
            Status = TripStatus.Draft
        };
        _db.Trips.Add(trip);

        var vehicle = new Vehicle
        {
            Model = "Test Van",
            PricePerDay = 5000,
            Status = VehicleStatus.Active,
            ProviderId = 20,
            Capacity = 4
        };
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        var dto = new CreateCheckoutDto
        {
            TripId = trip.Id,
            ProposalId = "test-prop",
            Vehicle = new VehicleCheckoutItemDto
            {
                VehicleId = vehicle.Id,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate
            }
        };

        // Act
        var checkout = await _checkoutService.PlaceApprovedProposalHoldAsync(dto, travelerId: 10);

        // Assert
        Assert.NotNull(checkout);
        Assert.NotNull(checkout.VehicleItem);
        // Should be 1 day * 5000 = 5000, instead of 0
        Assert.Equal(5000m, checkout.VehicleItem.PriceSnapshot);
    }
}
