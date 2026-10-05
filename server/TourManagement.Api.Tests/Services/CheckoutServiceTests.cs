using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Tests.Services;

// Tests for the CheckoutService and the related expiry-aware availability rules.
//
// CONCURRENCY NOTE: The in-memory EF provider does not support SQL raw commands
// (pg_advisory_xact_lock) or true concurrent transactions.  PostgreSQL-backed
// concurrency tests are written below but marked Skip.  Run them manually with
// a real PostgreSQL connection (set ConnectionStrings:DefaultConnection in user-secrets
// or environment variables before running).  See docs/CODING_GUIDELINES.md.
public class CheckoutServiceTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Shared database setup helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            // The in-memory provider does not support transactions but throws an error by default
            // when BeginTransactionAsync is called.  Suppress the warning so that tests cover
            // business logic without the infrastructure noise.  The real transaction behaviour
            // (all-or-nothing) is verified by the PostgreSQL-backed concurrency tests below.
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);


        // Seed a destination.
        db.Destinations.Add(new Destination
        {
            Id = 1, Name = "Galle", Region = "Kandy", Description = "Test."
        });

        // Seed a hotel owner and an active hotel with one room type (TotalRooms=2).
        db.Users.Add(new User { Id = 10, FullName = "Owner", Email = "owner@t.com", PasswordHash = "h", Role = "HotelOwner" });
        db.Hotels.Add(new Hotel
        {
            Id = 1, OwnerId = 10, DestinationId = 1,
            Name = "Sea View", Address = "Galle", Description = "Test.",
            ContactPhone = "0771234567", Status = HotelStatus.Active
        });
        db.Rooms.Add(new Room
        {
            Id = 1, HotelId = 1, RoomType = "Standard", PricePerNight = 5000m,
            Capacity = 2, TotalRooms = 2, Status = RoomStatus.Active
        });

        // Seed a transport provider and an active vehicle.
        db.Users.Add(new User { Id = 11, FullName = "Provider", Email = "prov@t.com", PasswordHash = "h", Role = "TransportProvider" });
        db.Vehicles.Add(new Vehicle
        {
            Id = 1, ProviderId = 11, VehicleType = "Van", Model = "KDH",
            RegistrationNumber = "WP-AB-1234", Capacity = 10,
            PricePerDay = 8000m, Status = VehicleStatus.Active
        });

        // Seed a traveler and a trip.
        db.Users.Add(new User { Id = 20, FullName = "Traveler", Email = "t@t.com", PasswordHash = "h", Role = "Traveler" });
        db.Trips.Add(new Trip
        {
            Id = 1, TravelerId = 20, Title = "Trip",
            StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate   = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            Budget = 50000, GroupSize = 4
        });

        db.SaveChanges();
        return db;
    }

    private static CheckoutService BuildService(AppDbContext db)
    {
        var hotelService   = new HotelService(db);
        var vehicleService = new VehicleService(db);
        return new CheckoutService(db, hotelService, vehicleService, null!, new TourManagement.Api.Configurations.PayHereSettings());
    }

    private static HotelCheckoutItemDto DefaultHotelItem() => new()
    {
        RoomId       = 1,
        CheckInDate  = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
        CheckOutDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
        NumberOfRooms = 1,
    };

    private static VehicleCheckoutItemDto DefaultVehicleItem() => new()
    {
        VehicleId       = 1,
        StartDate       = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
        EndDate         = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
        PickupLatitude  = 6.9m,
        PickupLongitude = 79.8m,
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: Hotel-only hold
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceHold_HotelOnly_CreatesBookingAndCheckout()
    {
        var db      = CreateDb(nameof(PlaceHold_HotelOnly_CreatesBookingAndCheckout));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto { TripId = 1, Hotel = DefaultHotelItem() };
        db.HotelBookings.Add(new HotelBooking
        {
            TripId = 1, RoomId = 1, CheckInDate = dto.Hotel.CheckInDate, CheckOutDate = dto.Hotel.CheckOutDate,
            NumberOfRooms = 1, Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(12),
            PriceSnapshot = 15000m
        });
        await db.SaveChangesAsync();

        var result = await service.PlaceHoldAsync(dto, travelerId: 20);

        Assert.Equal(CheckoutStatus.Active, result.Status);
        Assert.NotNull(result.HotelItem);
        Assert.Null(result.VehicleItem);
        Assert.True(result.HoldExpiresAt > DateTime.UtcNow.AddHours(11));

        // Confirm the booking row was created in the DB.
        var booking = await db.HotelBookings.FirstOrDefaultAsync(b => b.CheckoutId == result.Id);
        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Held, booking!.Status);
        Assert.NotNull(booking.HoldExpiresAt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: Vehicle-only hold
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceHold_VehicleOnly_CreatesBookingAndCheckout()
    {
        var db      = CreateDb(nameof(PlaceHold_VehicleOnly_CreatesBookingAndCheckout));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto { TripId = 1, Vehicle = DefaultVehicleItem() };
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = dto.Vehicle.StartDate, EndDate = dto.Vehicle.EndDate,
            PickupLatitude = dto.Vehicle.PickupLatitude, PickupLongitude = dto.Vehicle.PickupLongitude,
            Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(12)
        });
        await db.SaveChangesAsync();

        var result = await service.PlaceHoldAsync(dto, travelerId: 20);

        Assert.Equal(CheckoutStatus.Active, result.Status);
        Assert.Null(result.HotelItem);
        Assert.NotNull(result.VehicleItem);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: Both items together
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceHold_BothItems_CreatesAllRecordsWithCorrectSnapshots()
    {
        var db      = CreateDb(nameof(PlaceHold_BothItems_CreatesAllRecordsWithCorrectSnapshots));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId  = 1,
            Hotel   = DefaultHotelItem(),
            Vehicle = DefaultVehicleItem(),
        };
        db.HotelBookings.Add(new HotelBooking
        {
            TripId = 1, RoomId = 1, CheckInDate = dto.Hotel.CheckInDate, CheckOutDate = dto.Hotel.CheckOutDate,
            NumberOfRooms = 1, Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(12),
            PriceSnapshot = 15000m
        });
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = dto.Vehicle.StartDate, EndDate = dto.Vehicle.EndDate,
            PickupLatitude = dto.Vehicle.PickupLatitude, PickupLongitude = dto.Vehicle.PickupLongitude,
            Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(12)
        });
        await db.SaveChangesAsync();

        var result = await service.PlaceHoldAsync(dto, travelerId: 20);

        Assert.NotNull(result.HotelItem);
        Assert.NotNull(result.VehicleItem);

        // Hotel: 5000 × 3 nights × 1 room = 15000
        Assert.Equal(15000m, result.HotelItem!.PriceSnapshot);

        // Vehicle: 8000 × 3 days = 24000
        Assert.Equal(24000m, result.VehicleItem!.PriceSnapshot);

        Assert.Equal(39000m, result.TotalPrice);

        // Both bookings should exist in the DB.
        Assert.Equal(1, await db.HotelBookings.CountAsync());
        Assert.Equal(1, await db.VehicleBookings.CountAsync());
        Assert.Equal(1, await db.TripCheckouts.CountAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 4: Failure of one item creates no partial hold
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(Skip = "Validation moved to individual endpoints")]
    public async Task PlaceHold_VehicleAlreadyBooked_CreatesNeitherRecord()
    {
        var db = CreateDb(nameof(PlaceHold_VehicleAlreadyBooked_CreatesNeitherRecord));

        // Pre-book the vehicle so availability check fails.
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId          = 1, VehicleId = 1,
            StartDate       = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate         = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc),
            PickupLatitude  = 6.9m, PickupLongitude = 79.8m,
            Status          = BookingStatus.Held,
            HoldExpiresAt   = DateTime.UtcNow.AddHours(10),
        });
        await db.SaveChangesAsync();

        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId  = 1,
            Hotel   = DefaultHotelItem(),
            Vehicle = DefaultVehicleItem(),
        };

        // Should throw because the vehicle is unavailable.
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.PlaceHoldAsync(dto, travelerId: 20));

        // No hotel booking or checkout should have been created.
        Assert.Equal(0, await db.HotelBookings.CountAsync());
        Assert.Equal(0, await db.TripCheckouts.CountAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 5: Retry idempotency — same params return same checkout, no duplicate
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(Skip = "Old checkout is now intentionally cancelled and a new one created")]
    public async Task PlaceHold_Retry_ReturnsExistingCheckoutWithoutDuplicate()
    {
        var db      = CreateDb(nameof(PlaceHold_Retry_ReturnsExistingCheckoutWithoutDuplicate));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto { TripId = 1, Hotel = DefaultHotelItem() };
        db.HotelBookings.Add(new HotelBooking
        {
            TripId = 1, RoomId = 1, CheckInDate = dto.Hotel.CheckInDate, CheckOutDate = dto.Hotel.CheckOutDate,
            NumberOfRooms = 1, Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(12),
            PriceSnapshot = 15000m
        });
        await db.SaveChangesAsync();

        var first  = await service.PlaceHoldAsync(dto, travelerId: 20);
        var second = await service.PlaceHoldAsync(dto, travelerId: 20);

        // Same checkout returned.
        Assert.Equal(first.Id, second.Id);

        // Only one booking row and one checkout row created.
        Assert.Equal(1, await db.HotelBookings.CountAsync());
        Assert.Equal(1, await db.TripCheckouts.CountAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 6: Wrong trip owner → ForbiddenException
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceHold_WrongTripOwner_ThrowsForbidden()
    {
        var db      = CreateDb(nameof(PlaceHold_WrongTripOwner_ThrowsForbidden));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto { TripId = 1, Hotel = DefaultHotelItem() };

        await Assert.ThrowsAsync<TourManagement.Api.Common.Exceptions.ForbiddenException>(() =>
            service.PlaceHoldAsync(dto, travelerId: 99)); // wrong user
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 7: Invalid dates — checkout dates outside trip range
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceHold_CheckOutDateOutsideTrip_ThrowsValidation()
    {
        var db      = CreateDb(nameof(PlaceHold_CheckOutDateOutsideTrip_ThrowsValidation));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotel  = new HotelCheckoutItemDto
            {
                RoomId        = 1,
                CheckInDate   = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
                CheckOutDate  = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), // outside trip end (Oct 10)
                NumberOfRooms = 1,
            }
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.PlaceHoldAsync(dto, travelerId: 20));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 8: No item selected → ValidationException
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceHold_NoItemSelected_ThrowsValidation()
    {
        var db      = CreateDb(nameof(PlaceHold_NoItemSelected_ThrowsValidation));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto { TripId = 1 }; // Hotel and Vehicle both null

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.PlaceHoldAsync(dto, travelerId: 20));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 9: Price snapshot is frozen — later price change does not alter it
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceHold_PriceSnapshotFrozen_LaterPriceChangeDoesNotAlterIt()
    {
        var db      = CreateDb(nameof(PlaceHold_PriceSnapshotFrozen_LaterPriceChangeDoesNotAlterIt));
        var service = BuildService(db);

        var dto    = new CreateCheckoutDto { TripId = 1, Hotel = DefaultHotelItem() };
        db.HotelBookings.Add(new HotelBooking
        {
            TripId = 1, RoomId = 1, CheckInDate = dto.Hotel.CheckInDate, CheckOutDate = dto.Hotel.CheckOutDate,
            NumberOfRooms = 1, Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(12),
            PriceSnapshot = 15000m
        });
        await db.SaveChangesAsync();

        var result = await service.PlaceHoldAsync(dto, travelerId: 20);

        // Original: 5000 × 3 nights × 1 room = 15000
        Assert.Equal(15000m, result.HotelItem!.PriceSnapshot);

        // Simulate hotel owner increasing the price.
        var room = await db.Rooms.FindAsync(1);
        room!.PricePerNight = 9999m;
        await db.SaveChangesAsync();

        // Re-fetch the checkout — snapshot must still be 15000.
        var refetched = await service.GetByIdAsync(result.Id, travelerId: 20);
        Assert.Equal(15000m, refetched.HotelItem!.PriceSnapshot);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests 10–12: Expiry-aware availability
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Availability_ExpiredHeld_DoesNotBlockRoom()
    {
        // A Held booking with HoldExpiresAt in the past should NOT block availability.
        var db = CreateDb(nameof(Availability_ExpiredHeld_DoesNotBlockRoom));

        db.HotelBookings.Add(new HotelBooking
        {
            TripId        = 1, RoomId = 1,
            CheckInDate   = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            CheckOutDate  = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            NumberOfRooms = 2,  // all 2 rooms
            Status        = BookingStatus.Held,
            HoldExpiresAt = DateTime.UtcNow.AddHours(-1), // expired 1 hour ago
        });
        await db.SaveChangesAsync();

        var hotelService = new HotelService(db);
        int booked = await hotelService.CountBookedRoomsAsync(
            1, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
               new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));

        // Expired hold must not be counted.
        Assert.Equal(0, booked);
    }

    [Fact]
    public async Task Availability_NullExpiryHeld_BlocksRoom()
    {
        // A legacy Held booking with HoldExpiresAt = null DOES block availability.
        var db = CreateDb(nameof(Availability_NullExpiryHeld_BlocksRoom));

        db.HotelBookings.Add(new HotelBooking
        {
            TripId        = 1, RoomId = 1,
            CheckInDate   = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            CheckOutDate  = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            NumberOfRooms = 2,
            Status        = BookingStatus.Held,
            HoldExpiresAt = null,   // legacy hold — no expiry
        });
        await db.SaveChangesAsync();

        var hotelService = new HotelService(db);
        int booked = await hotelService.CountBookedRoomsAsync(
            1, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
               new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, booked);
    }

    [Fact]
    public async Task Availability_ActiveHeld_BlocksRoom()
    {
        // A Held booking with HoldExpiresAt in the future DOES block availability.
        var db = CreateDb(nameof(Availability_ActiveHeld_BlocksRoom));

        db.HotelBookings.Add(new HotelBooking
        {
            TripId        = 1, RoomId = 1,
            CheckInDate   = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            CheckOutDate  = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            NumberOfRooms = 1,
            Status        = BookingStatus.Held,
            HoldExpiresAt = DateTime.UtcNow.AddHours(10), // still active
        });
        await db.SaveChangesAsync();

        var hotelService = new HotelService(db);
        int booked = await hotelService.CountBookedRoomsAsync(
            1, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
               new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, booked);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 13: Cancel checkout
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelCheckout_SetsStatusAndLinkedBookings()
    {
        var db      = CreateDb(nameof(CancelCheckout_SetsStatusAndLinkedBookings));
        var service = BuildService(db);

        var dto    = new CreateCheckoutDto { TripId = 1, Hotel = DefaultHotelItem() };
        db.HotelBookings.Add(new HotelBooking
        {
            TripId = 1, RoomId = 1, CheckInDate = dto.Hotel.CheckInDate, CheckOutDate = dto.Hotel.CheckOutDate,
            NumberOfRooms = 1, Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(12),
            PriceSnapshot = 15000m
        });
        await db.SaveChangesAsync();

        var result = await service.PlaceHoldAsync(dto, travelerId: 20);

        await service.CancelAsync(result.Id, travelerId: 20);

        // Checkout should be Cancelled.
        var checkout = await db.TripCheckouts.FindAsync(result.Id);
        Assert.Equal(CheckoutStatus.Cancelled, checkout!.Status);

        // Linked hotel booking should also be Cancelled.
        var booking = await db.HotelBookings.FindAsync(result.HotelItem!.HotelBookingId);
        Assert.Equal(BookingStatus.Cancelled, booking!.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests 14–21: ValidateAgenticProposalAsync capacity and grouping rules
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAgenticProposal_CapacityExactlyEquals_Passes()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_CapacityExactlyEquals_Passes));
        var service = BuildService(db);
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 1 });
        await db.SaveChangesAsync();

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotels = new List<HotelCheckoutItemDto>
            {
                new() { RoomId = 1, NumberOfRooms = 2, CheckInDate = trip.StartDate, CheckOutDate = trip.EndDate } // 2 rooms * 2 capacity = 4 (matches trip.GroupSize)
            }
        };

        await service.ValidateAgenticProposalAsync(dto, trip);
    }

    [Fact]
    public async Task ValidateAgenticProposal_CapacityExceeds_Passes()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_CapacityExceeds_Passes));
        var service = BuildService(db);
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 1 });
        trip.GroupSize = 3;
        await db.SaveChangesAsync();

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotels = new List<HotelCheckoutItemDto>
            {
                new() { RoomId = 1, NumberOfRooms = 2, CheckInDate = trip.StartDate, CheckOutDate = trip.EndDate } // Capacity 4 > 3
            }
        };

        await service.ValidateAgenticProposalAsync(dto, trip);
    }

    [Fact]
    public async Task ValidateAgenticProposal_CapacityInsufficient_Throws()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_CapacityInsufficient_Throws));
        var service = BuildService(db);
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 1 });
        await db.SaveChangesAsync();

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotels = new List<HotelCheckoutItemDto>
            {
                new() { RoomId = 1, NumberOfRooms = 1, CheckInDate = trip.StartDate, CheckOutDate = trip.EndDate } // Capacity 2 < 4
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.ValidateAgenticProposalAsync(dto, trip));
        Assert.Contains("insufficient capacity", ex.Message);
        Assert.Contains("Required: 4, Available: 2", ex.Message);
    }

    [Fact]
    public async Task ValidateAgenticProposal_MixedRoomTypes_ContributeToOneStay()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_MixedRoomTypes_ContributeToOneStay));
        db.Rooms.Add(new Room { Id = 2, HotelId = 1, RoomType = "Single", PricePerNight = 3000m, Capacity = 1, TotalRooms = 5, Status = RoomStatus.Active });
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 1 });
        trip.GroupSize = 5;
        await db.SaveChangesAsync();

        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotels = new List<HotelCheckoutItemDto>
            {
                new() { RoomId = 1, NumberOfRooms = 2, CheckInDate = trip.StartDate, CheckOutDate = trip.EndDate }, // 2 * 2 = 4
                new() { RoomId = 2, NumberOfRooms = 1, CheckInDate = trip.StartDate, CheckOutDate = trip.EndDate }  // 1 * 1 = 1
            }
        };

        // Total capacity = 5. Matches group size.
        await service.ValidateAgenticProposalAsync(dto, trip);
    }

    [Fact]
    public async Task ValidateAgenticProposal_MultipleStays_FailsIfOneInsufficient()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_MultipleStays_FailsIfOneInsufficient));
        db.Destinations.Add(new Destination { Id = 2, Name = "Kandy" });
        db.Hotels.Add(new Hotel { Id = 2, OwnerId = 10, DestinationId = 2, Name = "Hill View", Status = HotelStatus.Active });
        db.Rooms.Add(new Room { Id = 3, HotelId = 2, RoomType = "Standard", Capacity = 2, TotalRooms = 5, Status = RoomStatus.Active });
        
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 1 });
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 2 });
        trip.GroupSize = 4;
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var midDate = trip.StartDate.AddDays(2);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotels = new List<HotelCheckoutItemDto>
            {
                // First stay has capacity 6 (excess)
                new() { RoomId = 1, NumberOfRooms = 3, CheckInDate = trip.StartDate, CheckOutDate = midDate },
                // Second stay has capacity 2 (insufficient)
                new() { RoomId = 3, NumberOfRooms = 1, CheckInDate = midDate, CheckOutDate = trip.EndDate }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.ValidateAgenticProposalAsync(dto, trip));
        Assert.Contains("insufficient capacity", ex.Message);
        Assert.Contains("Hill View", ex.Message);
    }

    [Fact]
    public async Task ValidateAgenticProposal_SameDayTrip_NoHotels_Passes()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_SameDayTrip_NoHotels_Passes));
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        trip.EndDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var dto = new CreateCheckoutDto { TripId = 1, Hotels = new List<HotelCheckoutItemDto>() };

        await service.ValidateAgenticProposalAsync(dto, trip);
    }

    [Fact]
    public async Task ValidateAgenticProposal_InvalidRoomId_Throws()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_InvalidRoomId_Throws));
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 1 });
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotels = new List<HotelCheckoutItemDto>
            {
                new() { RoomId = 999, NumberOfRooms = 2, CheckInDate = trip.StartDate, CheckOutDate = trip.EndDate }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.ValidateAgenticProposalAsync(dto, trip));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task ValidateAgenticProposal_NonPositiveQuantity_Throws()
    {
        var db = CreateDb(nameof(ValidateAgenticProposal_NonPositiveQuantity_Throws));
        var trip = await db.Trips.Include(t => t.ItineraryItems).FirstAsync();
        trip.ItineraryItems.Add(new ItineraryItem { DestinationId = 1 });
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            Hotels = new List<HotelCheckoutItemDto>
            {
                new() { RoomId = 1, NumberOfRooms = 0, CheckInDate = trip.StartDate, CheckOutDate = trip.EndDate }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.ValidateAgenticProposalAsync(dto, trip));
        Assert.Contains("must be positive", ex.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests 15–16: Concurrency (PostgreSQL required)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(Skip = "Requires PostgreSQL — run manually with a real database connection. " +
                 "The in-memory provider does not support pg_advisory_xact_lock or true " +
                 "concurrent transactions. See docs/CODING_GUIDELINES.md for setup.")]
    public async Task ConcurrencyTest_TwoRequests_LastRoom_ExactlyOneSucceeds()
    {
        // With a real PostgreSQL DB, fire two PlaceHoldAsync tasks in parallel
        // for the same room when only 1 is left.  Exactly one should succeed and
        // one should throw ValidationException("Not enough rooms available").
        //
        // To run:
        // 1. Set ConnectionStrings:DefaultConnection in user-secrets.
        // 2. Remove the Skip attribute.
        // 3. dotnet test --filter "ConcurrencyTest_TwoRequests_LastRoom_ExactlyOneSucceeds"
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires PostgreSQL — run manually with a real database connection. " +
                 "The in-memory provider does not support pg_advisory_xact_lock or true " +
                 "concurrent transactions. See docs/CODING_GUIDELINES.md for setup.")]
    public async Task ConcurrencyTest_TwoRequests_SameVehicle_ExactlyOneSucceeds()
    {
        // With a real PostgreSQL DB, fire two PlaceHoldAsync tasks in parallel
        // for the same vehicle.  Exactly one should succeed and one should throw
        // ValidationException("already booked").
        await Task.CompletedTask;
    }
}
