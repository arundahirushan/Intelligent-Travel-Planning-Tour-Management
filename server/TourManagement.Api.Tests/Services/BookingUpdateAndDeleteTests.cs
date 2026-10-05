using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Dtos.SupplyOrders;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using Xunit;

namespace TourManagement.Api.Tests.Services;

public class BookingUpdateAndDeleteTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new AppDbContext(options);

        // Standard setup
        db.Users.Add(new User { Id = 1, FullName = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = Roles.Admin, Status = UserStatus.Active });
        db.Users.Add(new User { Id = 2, FullName = "Traveler", Email = "traveler@test.com", PasswordHash = "hash", Role = Roles.Traveler, Status = UserStatus.Active });
        db.Users.Add(new User { Id = 3, FullName = "Supplier", Email = "s@test.com", PasswordHash = "hash", Role = Roles.Supplier, Status = UserStatus.Active });
        db.Users.Add(new User { Id = 4, FullName = "Traveler 2", Email = "t2@test.com", PasswordHash = "hash", Role = Roles.Traveler, Status = UserStatus.Active });
        
        var trip = new Trip { Id = 1, TravelerId = 2, Title = "My Trip", StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(20), Status = TripStatus.Draft };
        db.Trips.Add(trip);
        db.Trips.Add(new Trip { Id = 2, TravelerId = 2, Title = "My Trip 2", StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(20), Status = TripStatus.Draft });
        db.Trips.Add(new Trip { Id = 3, TravelerId = 4, Title = "Another Traveler Trip", StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(20), Status = TripStatus.Draft });

        var destination = new Destination { Id = 1, Name = "Galle", Region = "South", Description = "Desc" };
        db.Destinations.Add(destination);

        var hotel = new Hotel { Id = 1, DestinationId = 1, OwnerId = 1, Name = "Test Hotel", Status = HotelStatus.Active };
        db.Hotels.Add(hotel);

        var room1 = new Room { Id = 1, HotelId = 1, RoomType = "Standard", TotalRooms = 1, PricePerNight = 100, Capacity = 2, Status = RoomStatus.Active };
        var room2 = new Room { Id = 2, HotelId = 1, RoomType = "Deluxe", TotalRooms = 1, PricePerNight = 200, Capacity = 2, Status = RoomStatus.Active };
        db.Rooms.Add(room1);
        db.Rooms.Add(room2);

        var vehicle1 = new Vehicle { Id = 1, ProviderId = 1, RegistrationNumber = "V1", VehicleType = "Car", Model = "Toyota", Capacity = 4, PricePerDay = 50, Status = VehicleStatus.Active };
        var vehicle2 = new Vehicle { Id = 2, ProviderId = 1, RegistrationNumber = "V2", VehicleType = "Van", Model = "Ford", Capacity = 8, PricePerDay = 100, Status = VehicleStatus.Active };
        db.Vehicles.Add(vehicle1);
        db.Vehicles.Add(vehicle2);

        var contract = new Contract { Id = 1, SupplierId = 3, Status = ContractStatus.Active, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30) };
        db.Contracts.Add(contract);

        var supply1 = new Supply { Id = 1, SupplierId = 3, Name = "Water", PricePerUnit = 2, StockQuantity = 100, Status = SupplyStatus.Active };
        var supply2 = new Supply { Id = 2, SupplierId = 3, Name = "Tent", PricePerUnit = 50, StockQuantity = 10, Status = SupplyStatus.Active };
        db.Supplies.Add(supply1);
        db.Supplies.Add(supply2);

        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task UpdateBooking_ToOverlappingDifferentRoom_IsRejected()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var hotelService = new HotelService(db);
        var bookingService = new HotelBookingService(db, hotelService, new CheckoutService(db, hotelService, new VehicleService(db), null!, new TourManagement.Api.Configurations.PayHereSettings()));

        // Pre-book room 2 completely for days 12-14
        db.HotelBookings.Add(new HotelBooking { Id = 10, TripId = 1, RoomId = 2, CheckInDate = DateTime.UtcNow.AddDays(12), CheckOutDate = DateTime.UtcNow.AddDays(14), NumberOfRooms = 1, Status = BookingStatus.Confirmed });
        
        // Traveler has booking for room 1 on days 12-14
        var travelerBooking = new HotelBooking { Id = 11, TripId = 1, RoomId = 1, CheckInDate = DateTime.UtcNow.AddDays(12), CheckOutDate = DateTime.UtcNow.AddDays(14), NumberOfRooms = 1, Status = BookingStatus.Held };
        db.HotelBookings.Add(travelerBooking);
        await db.SaveChangesAsync();

        var dto = new UpdateHotelBookingDto { RoomId = 2, CheckInDate = DateTime.UtcNow.AddDays(12), CheckOutDate = DateTime.UtcNow.AddDays(14), NumberOfRooms = 1 };

        // Attempting to move booking to Room 2 which is already booked by someone else should fail
        var ex = await Assert.ThrowsAsync<ValidationException>(() => bookingService.UpdateAsync(11, dto, 2));
        Assert.Contains("Not enough rooms available", ex.Message);
    }

    [Fact]
    public async Task UpdateBooking_ToSelf_IsAllowed()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var hotelService = new HotelService(db);
        var bookingService = new HotelBookingService(db, hotelService, new CheckoutService(db, hotelService, new VehicleService(db), null!, new TourManagement.Api.Configurations.PayHereSettings()));

        var checkIn = DateTime.UtcNow.AddDays(12);
        var checkOut = DateTime.UtcNow.AddDays(14);
        
        var travelerBooking = new HotelBooking { Id = 11, TripId = 1, RoomId = 1, CheckInDate = checkIn, CheckOutDate = checkOut, NumberOfRooms = 1, Status = BookingStatus.Held };
        db.HotelBookings.Add(travelerBooking);
        await db.SaveChangesAsync();

        var dto = new UpdateHotelBookingDto { RoomId = 1, CheckInDate = checkIn, CheckOutDate = checkOut, NumberOfRooms = 1 };

        // Should succeed because exclude-self logic works (room 1 only has 1 capacity, if it included itself it would show 0 available)
        var result = await bookingService.UpdateAsync(11, dto, 2);
        Assert.Equal("Standard", result.RoomType);
    }

    [Fact]
    public async Task UpdateOrDelete_OnCancelledBooking_IsRejected()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var hotelService = new HotelService(db);
        var bookingService = new HotelBookingService(db, hotelService, new CheckoutService(db, hotelService, new VehicleService(db), null!, new TourManagement.Api.Configurations.PayHereSettings()));

        var travelerBooking = new HotelBooking { Id = 11, TripId = 1, RoomId = 1, CheckInDate = DateTime.UtcNow.AddDays(12), CheckOutDate = DateTime.UtcNow.AddDays(14), NumberOfRooms = 1, Status = BookingStatus.Cancelled };
        db.HotelBookings.Add(travelerBooking);
        await db.SaveChangesAsync();

        var dto = new UpdateHotelBookingDto { RoomId = 1, CheckInDate = DateTime.UtcNow.AddDays(12), CheckOutDate = DateTime.UtcNow.AddDays(14), NumberOfRooms = 1 };

        var exUpdate = await Assert.ThrowsAsync<ValidationException>(() => bookingService.UpdateAsync(11, dto, 2));
        Assert.Contains("already Cancelled", exUpdate.Message);

        var exDelete = await Assert.ThrowsAsync<ValidationException>(() => bookingService.DeleteAsync(11, 2, Roles.Traveler));
        Assert.Contains("already Cancelled", exDelete.Message);
    }

    [Fact]
    public async Task DeleteSupplyOrder_RestoresStockAndRemovesRow()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var contractService = new ContractService(db);
        var supplyService = new SupplyOrderService(db, contractService);

        // supply1 stock initially 100 in CreateDb
        var order = new SupplyOrder { Id = 11, TripId = 1, SupplyId = 1, Quantity = 20, Status = BookingStatus.Held, PriceAtOrderTime = 2 };
        db.SupplyOrders.Add(order);
        await db.SaveChangesAsync();

        await supplyService.DeleteAsync(11, 2, Roles.Traveler);

        var supply = await db.Supplies.FindAsync(1);
        Assert.Equal(120, supply!.StockQuantity); // Restored + 20

        var deletedOrder = await db.SupplyOrders.FindAsync(11);
        Assert.Null(deletedOrder); // Hard deleted
    }

    [Fact]
    public async Task UpdateSupplyOrder_ToDifferentSupply_RestoresAndDecrements()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var contractService = new ContractService(db);
        var supplyService = new SupplyOrderService(db, contractService);

        // supply1 stock 100, supply2 stock 10
        var order = new SupplyOrder { Id = 11, TripId = 1, SupplyId = 1, Quantity = 20, Status = BookingStatus.Held, PriceAtOrderTime = 2 };
        db.SupplyOrders.Add(order);
        await db.SaveChangesAsync();

        var dto = new UpdateSupplyOrderDto { SupplyId = 2, Quantity = 5 };
        
        await supplyService.UpdateAsync(11, dto, 2);

        var supply1 = await db.Supplies.FindAsync(1);
        var supply2 = await db.Supplies.FindAsync(2);

        Assert.Equal(120, supply1!.StockQuantity); // Old stock restored (100 + 20)
        Assert.Equal(5, supply2!.StockQuantity);   // New stock decremented (10 - 5)

        // Ensure order updated
        var updatedOrder = await db.SupplyOrders.FindAsync(11);
        Assert.Equal(2, updatedOrder!.SupplyId);
        Assert.Equal(5, updatedOrder.Quantity);
    }
    [Fact]
    public async Task GetMyBookingsAsync_Hotel_FiltersByTripId_WhenProvided()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var hotelService = new HotelService(db);
        var bookingService = new HotelBookingService(db, hotelService, new CheckoutService(db, hotelService, new VehicleService(db), null!, new TourManagement.Api.Configurations.PayHereSettings()));

        db.HotelBookings.Add(new HotelBooking { Id = 1, TripId = 1, RoomId = 1, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow.AddDays(1) });
        db.HotelBookings.Add(new HotelBooking { Id = 2, TripId = 2, RoomId = 1, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow.AddDays(1) });
        db.HotelBookings.Add(new HotelBooking { Id = 3, TripId = 3, RoomId = 1, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow.AddDays(1) }); // Another traveler
        await db.SaveChangesAsync();

        // 1. Without tripId, returns all for traveler 2 (Trip 1 and 2)
        var allMy = await bookingService.GetMyBookingsAsync(2, null, null, 1, 10);
        Assert.Equal(2, allMy.Items.Count);

        // 2. With tripId = 1, returns only Trip 1
        var trip1My = await bookingService.GetMyBookingsAsync(2, 1, null, 1, 10);
        Assert.Single(trip1My.Items);
        Assert.Equal(1, trip1My.Items[0].Id);
        
        // 3. Requesting another traveler's tripId (3) returns empty (ownership check)
        var trip3My = await bookingService.GetMyBookingsAsync(2, 3, null, 1, 10);
        Assert.Empty(trip3My.Items);
    }

    [Fact]
    public async Task GetMyBookingsAsync_Vehicle_FiltersByTripId_WhenProvided()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var vehicleService = new VehicleBookingService(db, new VehicleService(db), new CheckoutService(db, new HotelService(db), new VehicleService(db), null!, new TourManagement.Api.Configurations.PayHereSettings()));

        db.VehicleBookings.Add(new VehicleBooking { Id = 1, TripId = 1, VehicleId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) });
        db.VehicleBookings.Add(new VehicleBooking { Id = 2, TripId = 2, VehicleId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) });
        db.VehicleBookings.Add(new VehicleBooking { Id = 3, TripId = 3, VehicleId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) }); // Another traveler
        await db.SaveChangesAsync();

        var allMy = await vehicleService.GetMyBookingsAsync(2, null, 1, 10);
        Assert.Equal(2, allMy.Items.Count);

        var trip1My = await vehicleService.GetMyBookingsAsync(2, 1, 1, 10);
        Assert.Single(trip1My.Items);
        Assert.Equal(1, trip1My.Items[0].Id);
        
        var trip3My = await vehicleService.GetMyBookingsAsync(2, 3, 1, 10);
        Assert.Empty(trip3My.Items);
    }

    [Fact]
    public async Task GetMyOrdersAsync_Supply_FiltersByTripId_WhenProvided()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var supplyService = new SupplyOrderService(db, new ContractService(db));

        db.SupplyOrders.Add(new SupplyOrder { Id = 1, TripId = 1, SupplyId = 1, Quantity = 1 });
        db.SupplyOrders.Add(new SupplyOrder { Id = 2, TripId = 2, SupplyId = 1, Quantity = 1 });
        db.SupplyOrders.Add(new SupplyOrder { Id = 3, TripId = 3, SupplyId = 1, Quantity = 1 }); // Another traveler
        await db.SaveChangesAsync();

        var allMy = await supplyService.GetMyOrdersAsync(2, null, null, null, 1, 10);
        Assert.Equal(2, allMy.Items.Count);

        var trip1My = await supplyService.GetMyOrdersAsync(2, 1, null, null, 1, 10);
        Assert.Single(trip1My.Items);
        Assert.Equal(1, trip1My.Items[0].Id);
        
        var trip3My = await supplyService.GetMyOrdersAsync(2, 3, null, null, 1, 10);
        Assert.Empty(trip3My.Items);
    }
}
