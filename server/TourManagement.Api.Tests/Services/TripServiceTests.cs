using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using Xunit;

namespace TourManagement.Api.Tests.Services;

public class TripServiceTests
{
    private AppDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    private TripService GetService(AppDbContext db)
    {
        return new TripService(db);
    }

    [Fact]
    public async Task DeleteAsync_EmptyUnpaidTrip_PhysicallyRemoved()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = GetDbContext(dbName);
        var service = GetService(db);

        var trip = new Trip { Id = 1, TravelerId = 100, Status = TripStatus.Draft, Title = "Test Trip", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        await service.DeleteAsync(1, 100);

        var tripInDb = await db.Trips.FindAsync(1);
        Assert.Null(tripInDb);
    }

    [Fact]
    public async Task DeleteAsync_HeldBookings_ReleaseAvailabilityAndRestoreStock()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = GetDbContext(dbName);
        var service = GetService(db);

        var trip = new Trip { Id = 1, TravelerId = 100, Status = TripStatus.Planned, Title = "Test Trip", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        var supply = new Supply { Id = 1, SupplierId = 10, StockQuantity = 50, Status = SupplyStatus.Active, Name = "Water" };
        var supplyOrder = new SupplyOrder { Id = 1, TripId = 1, SupplyId = 1, Quantity = 10, Status = BookingStatus.Held, PriceAtOrderTime = 100 };
        var hotelBooking = new HotelBooking { Id = 1, TripId = 1, RoomId = 1, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow.AddDays(1), NumberOfRooms = 1, Status = BookingStatus.Held };
        
        db.Trips.Add(trip);
        db.Supplies.Add(supply);
        db.SupplyOrders.Add(supplyOrder);
        db.HotelBookings.Add(hotelBooking);
        await db.SaveChangesAsync();

        await service.DeleteAsync(1, 100);

        var tripInDb = await db.Trips.FindAsync(1);
        Assert.Null(tripInDb);

        var supplyInDb = await db.Supplies.FindAsync(1);
        Assert.Equal(60, supplyInDb.StockQuantity);

        var hotelBookingInDb = await db.HotelBookings.FindAsync(1);
        Assert.Null(hotelBookingInDb);
    }

    [Fact]
    public async Task DeleteAsync_CancelledExpiredBookings_DoNotRestoreStockTwice()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = GetDbContext(dbName);
        var service = GetService(db);

        var trip = new Trip { Id = 1, TravelerId = 100, Status = TripStatus.Planned, Title = "Test Trip", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        var supply = new Supply { Id = 1, SupplierId = 10, StockQuantity = 50, Status = SupplyStatus.Active, Name = "Water" };
        var supplyOrder = new SupplyOrder { Id = 1, TripId = 1, SupplyId = 1, Quantity = 10, Status = BookingStatus.Cancelled, PriceAtOrderTime = 100 }; // Already cancelled
        
        db.Trips.Add(trip);
        db.Supplies.Add(supply);
        db.SupplyOrders.Add(supplyOrder);
        await db.SaveChangesAsync();

        await service.DeleteAsync(1, 100);

        var supplyInDb = await db.Supplies.FindAsync(1);
        Assert.Equal(50, supplyInDb.StockQuantity); // Should not restore stock again
    }

    [Fact]
    public async Task DeleteAsync_MultipleBookingsAndMixedResources_RemovedCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = GetDbContext(dbName);
        var service = GetService(db);

        var trip = new Trip { Id = 1, TravelerId = 100, Status = TripStatus.Planned, Title = "Test Trip", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        var hotelBooking1 = new HotelBooking { Id = 1, TripId = 1, RoomId = 1, Status = BookingStatus.Held, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow };
        var hotelBooking2 = new HotelBooking { Id = 2, TripId = 1, RoomId = 2, Status = BookingStatus.Held, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow };
        var vehicleBooking = new VehicleBooking { Id = 1, TripId = 1, VehicleId = 1, Status = BookingStatus.Held, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };
        
        db.Trips.Add(trip);
        db.HotelBookings.Add(hotelBooking1);
        db.HotelBookings.Add(hotelBooking2);
        db.VehicleBookings.Add(vehicleBooking);
        await db.SaveChangesAsync();

        await service.DeleteAsync(1, 100);

        Assert.Empty(db.HotelBookings.ToList());
        Assert.Empty(db.VehicleBookings.ToList());
        Assert.Empty(db.Trips.ToList());
    }

    [Fact]
    public async Task DeleteAsync_PaidCheckout_BlocksDeletion()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = GetDbContext(dbName);
        var service = GetService(db);

        var trip = new Trip { Id = 1, TravelerId = 100, Status = TripStatus.Planned, Title = "Test Trip", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        var checkout = new TripCheckout { Id = 1, TripId = 1, Status = CheckoutStatus.Paid, TotalPrice = 100, TravelerId = 100 };
        
        db.Trips.Add(trip);
        db.TripCheckouts.Add(checkout);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(1, 100));
    }

    [Fact]
    public async Task DeleteAsync_WrongTraveler_ThrowsForbidden()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = GetDbContext(dbName);
        var service = GetService(db);

        var trip = new Trip { Id = 1, TravelerId = 100, Status = TripStatus.Draft, Title = "Test Trip", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(1, 200));
    }

    [Fact]
    public async Task DeleteAsync_RelatedRowsHandledWithoutDeletingCatalogueRecords()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = GetDbContext(dbName);
        var service = GetService(db);

        var trip = new Trip { Id = 1, TravelerId = 100, Status = TripStatus.Planned, Title = "Test Trip", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        var dest = new Destination { Id = 1, Name = "Colombo" };
        var hotel = new Hotel { Id = 1, DestinationId = 1, Name = "Hotel", Status = HotelStatus.Active, OwnerId = 10 };
        var room = new Room { Id = 1, HotelId = 1, RoomType = "Suite" };
        var hotelBooking = new HotelBooking { Id = 1, TripId = 1, RoomId = 1, Status = BookingStatus.Held, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow };

        db.Trips.Add(trip);
        db.Destinations.Add(dest);
        db.Hotels.Add(hotel);
        db.Rooms.Add(room);
        db.HotelBookings.Add(hotelBooking);
        await db.SaveChangesAsync();

        await service.DeleteAsync(1, 100);

        Assert.Empty(db.Trips.ToList());
        Assert.Empty(db.HotelBookings.ToList());
        // Catalogue records should remain untouched (in-memory DB doesn't cascade arbitrarily, but we prove we didn't remove them)
        Assert.Single(db.Destinations.ToList());
        Assert.Single(db.Hotels.ToList());
        Assert.Single(db.Rooms.ToList());
    }
}
