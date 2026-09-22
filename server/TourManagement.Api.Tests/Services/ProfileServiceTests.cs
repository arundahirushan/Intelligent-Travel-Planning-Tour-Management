using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

namespace TourManagement.Api.Tests.Services;

public class ProfileServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // ── HotelOwner ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckDeletionEligibility_HotelOwner_NoBlockingBookings_CanDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_HotelOwner_NoBlockingBookings_CanDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.HotelOwner });
        
        // 5 hotels, zero bookings
        for (int i = 1; i <= 5; i++)
        {
            db.Hotels.Add(new Hotel { Id = i, OwnerId = 1 });
            db.Rooms.Add(new Room { Id = i, HotelId = i });
        }
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.HotelOwner);

        Assert.True(result.CanDelete);
        Assert.Equal(0, result.BlockingCount);
    }

    [Fact]
    public async Task CheckDeletionEligibility_HotelOwner_OneBlockingBooking_CannotDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_HotelOwner_OneBlockingBooking_CannotDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.HotelOwner });
        db.Hotels.Add(new Hotel { Id = 1, OwnerId = 1 });
        db.Rooms.Add(new Room { Id = 1, HotelId = 1 });
        
        // CheckOutDate tomorrow = blocking
        db.HotelBookings.Add(new HotelBooking 
        { 
            Id = 1, RoomId = 1, 
            Status = BookingStatus.Held, 
            CheckOutDate = DateTime.UtcNow.Date.AddDays(1) 
        });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.HotelOwner);

        Assert.False(result.CanDelete);
        Assert.Equal(1, result.BlockingCount);
        Assert.Contains("1 active or upcoming hotel booking", result.BlockingMessage);
    }

    [Fact]
    public async Task CheckDeletionEligibility_HotelOwner_BookingAlreadyEnded_CanDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_HotelOwner_BookingAlreadyEnded_CanDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.HotelOwner });
        db.Hotels.Add(new Hotel { Id = 1, OwnerId = 1 });
        db.Rooms.Add(new Room { Id = 1, HotelId = 1 });
        
        // CheckOutDate yesterday = not blocking
        db.HotelBookings.Add(new HotelBooking 
        { 
            Id = 1, RoomId = 1, 
            Status = BookingStatus.Confirmed, 
            CheckOutDate = DateTime.UtcNow.Date.AddDays(-1) 
        });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.HotelOwner);

        Assert.True(result.CanDelete);
    }

    // ── TransportProvider ──────────────────────────────────────────────────

    [Fact]
    public async Task CheckDeletionEligibility_TransportProvider_NoBlocking_CanDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_TransportProvider_NoBlocking_CanDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.TransportProvider });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.TransportProvider);

        Assert.True(result.CanDelete);
    }

    [Fact]
    public async Task CheckDeletionEligibility_TransportProvider_OneBlocking_CannotDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_TransportProvider_OneBlocking_CannotDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.TransportProvider });
        db.Vehicles.Add(new Vehicle { Id = 1, ProviderId = 1 });
        
        db.VehicleBookings.Add(new VehicleBooking 
        { 
            Id = 1, VehicleId = 1, 
            Status = BookingStatus.Confirmed, 
            EndDate = DateTime.UtcNow.Date.AddDays(1) 
        });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.TransportProvider);

        Assert.False(result.CanDelete);
        Assert.Equal(1, result.BlockingCount);
        Assert.Contains("1 active or upcoming vehicle booking", result.BlockingMessage);
    }

    [Fact]
    public async Task CheckDeletionEligibility_TransportProvider_Ended_CanDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_TransportProvider_Ended_CanDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.TransportProvider });
        db.Vehicles.Add(new Vehicle { Id = 1, ProviderId = 1 });
        
        db.VehicleBookings.Add(new VehicleBooking 
        { 
            Id = 1, VehicleId = 1, 
            Status = BookingStatus.Confirmed, 
            EndDate = DateTime.UtcNow.Date.AddDays(-1) 
        });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.TransportProvider);

        Assert.True(result.CanDelete);
    }

    // ── Supplier ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckDeletionEligibility_Supplier_NoBlocking_CanDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_Supplier_NoBlocking_CanDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.Supplier });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.Supplier);

        Assert.True(result.CanDelete);
    }

    [Fact]
    public async Task CheckDeletionEligibility_Supplier_OneBlocking_CannotDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_Supplier_OneBlocking_CannotDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.Supplier });
        db.Supplies.Add(new Supply { Id = 1, SupplierId = 1 });
        db.Trips.Add(new Trip { Id = 1, EndDate = DateTime.UtcNow.Date.AddDays(1) });
        
        db.SupplyOrders.Add(new SupplyOrder 
        { 
            Id = 1, SupplyId = 1, TripId = 1,
            Status = BookingStatus.Held 
        });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.Supplier);

        Assert.False(result.CanDelete);
        Assert.Equal(1, result.BlockingCount);
        Assert.Contains("1 active or upcoming supply order", result.BlockingMessage);
    }

    [Fact]
    public async Task CheckDeletionEligibility_Supplier_Ended_CanDelete()
    {
        var db = CreateDb(nameof(CheckDeletionEligibility_Supplier_Ended_CanDelete));
        db.Users.Add(new User { Id = 1, Role = Roles.Supplier });
        db.Supplies.Add(new Supply { Id = 1, SupplierId = 1 });
        db.Trips.Add(new Trip { Id = 1, EndDate = DateTime.UtcNow.Date.AddDays(-1) });
        
        db.SupplyOrders.Add(new SupplyOrder 
        { 
            Id = 1, SupplyId = 1, TripId = 1,
            Status = BookingStatus.Held 
        });
        await db.SaveChangesAsync();

        var service = new ProfileService(db);
        var result = await service.CheckDeletionEligibilityAsync(1, Roles.Supplier);

        Assert.True(result.CanDelete);
    }
}
