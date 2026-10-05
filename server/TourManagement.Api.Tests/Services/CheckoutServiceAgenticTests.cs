using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using TourManagement.Api.Common.Exceptions;

namespace TourManagement.Api.Tests.Services;

public class CheckoutServiceAgenticTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);

        // Seed areas
        db.Destinations.AddRange(
            new Destination { Id = 1, Name = "Kandy Area" },
            new Destination { Id = 2, Name = "Matara Area" },
            new Destination { Id = 3, Name = "Colombo Area" }
        );

        // Seed hotels and rooms for each area
        db.Users.Add(new User { Id = 10, FullName = "Owner", Email = "o@t.com", Role = "HotelOwner" });
        db.Hotels.AddRange(
            new Hotel { Id = 1, OwnerId = 10, DestinationId = 1, Name = "Kandy Hotel", Status = HotelStatus.Active },
            new Hotel { Id = 2, OwnerId = 10, DestinationId = 2, Name = "Matara Hotel", Status = HotelStatus.Active },
            new Hotel { Id = 3, OwnerId = 10, DestinationId = 3, Name = "Colombo Hotel", Status = HotelStatus.Active }
        );
        db.Rooms.AddRange(
            new Room { Id = 1, HotelId = 1, RoomType = "Standard", PricePerNight = 5000m, Capacity = 2, TotalRooms = 2, Status = RoomStatus.Active },
            new Room { Id = 2, HotelId = 2, RoomType = "Standard", PricePerNight = 6000m, Capacity = 2, TotalRooms = 2, Status = RoomStatus.Active },
            new Room { Id = 3, HotelId = 3, RoomType = "Standard", PricePerNight = 7000m, Capacity = 2, TotalRooms = 2, Status = RoomStatus.Active }
        );

        db.Users.Add(new User { Id = 20, FullName = "Traveler", Email = "trav@t.com", Role = "Traveler" });
        
        // Seed Trip in Kandy and Matara
        db.Trips.Add(new Trip
        {
            Id = 1, TravelerId = 20, StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), Status = TripStatus.Planned, Budget = 100000m
        });
        db.ItineraryItems.AddRange(
            new ItineraryItem { Id = 1, TripId = 1, DestinationId = 1, DayNumber = 1 },
            new ItineraryItem { Id = 2, TripId = 1, DestinationId = 2, DayNumber = 2 }
        );

        db.Users.Add(new User { Id = 11, FullName = "Provider", Email = "prov@t.com", Role = "TransportProvider" });
        db.Vehicles.Add(new Vehicle { Id = 1, ProviderId = 11, RegistrationNumber = "V-123", PricePerDay = 3000m, Capacity = 4, Status = VehicleStatus.Active });

        db.SaveChanges();
        return db;
    }

    private static CheckoutService BuildService(AppDbContext db)
    {
        return new CheckoutService(db, new HotelService(db), new VehicleService(db), null!, new TourManagement.Api.Configurations.PayHereSettings());
    }

    [Fact]
    public async Task AgenticHold_TwoHotelsAndVehicle_Success()
    {
        var db = CreateDb(nameof(AgenticHold_TwoHotelsAndVehicle_Success));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-success",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            },
            Vehicle = new VehicleCheckoutItemDto { VehicleId = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 8) }
        };

        var result = await service.PlaceApprovedProposalHoldAsync(dto, 20);

        Assert.Equal(CheckoutStatus.Active, result.Status);
        Assert.Equal(2, result.Hotels.Count);
        Assert.NotNull(result.VehicleItem);
        Assert.Equal(15000m + 36000m + 21000m, result.TotalPrice); // 3 nights @5000 + 6 nights @6000 + 7 days @3000
    }

    [Fact]
    public async Task AgenticHold_WrongArea_Throws()
    {
        var db = CreateDb(nameof(AgenticHold_WrongArea_Throws));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-wrong-area",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 3, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.PlaceApprovedProposalHoldAsync(dto, 20));
        Assert.Contains("not selected for this trip", ex.Message);
    }

    [Fact]
    public async Task AgenticHold_DuplicateArea_Throws()
    {
        var db = CreateDb(nameof(AgenticHold_DuplicateArea_Throws));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-dup-area",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 3), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 6), NumberOfRooms = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.PlaceApprovedProposalHoldAsync(dto, 20));
        Assert.Contains("Cannot propose multiple separate hotel stays for the same area", ex.Message);
    }

    [Fact]
    public async Task AgenticHold_OverlappingDates_Throws()
    {
        var db = CreateDb(nameof(AgenticHold_OverlappingDates_Throws));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-overlap",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 5), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 8), NumberOfRooms = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.PlaceApprovedProposalHoldAsync(dto, 20));
        Assert.Contains("contiguous with no gaps or overlap", ex.Message);
    }

    [Fact]
    public async Task AgenticHold_OneUnavailable_ZeroHolds()
    {
        var db = CreateDb(nameof(AgenticHold_OneUnavailable_ZeroHolds));
        var service = BuildService(db);

        // Make room 2 unavailable by booking it fully
        db.HotelBookings.Add(new HotelBooking
        {
            TripId = 1, RoomId = 2, NumberOfRooms = 2, Status = BookingStatus.Confirmed,
            CheckInDate = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc),
            CheckOutDate = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-unavailable",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 8), NumberOfRooms = 1 }
            }
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.PlaceApprovedProposalHoldAsync(dto, 20));

        // Verify zero holds created
        Assert.Empty(db.TripCheckouts);
        // Room 1 should not have a held booking
        Assert.Empty(db.HotelBookings.Where(b => b.RoomId == 1));
    }

    [Fact]
    public async Task AgenticHold_MissingArea_Succeeds()
    {
        var db = CreateDb(nameof(AgenticHold_MissingArea_Succeeds));
        var service = BuildService(db);

        // Trip 1 has areas 1 and 2. We only provide hotel for area 1.
        // As per Option B, missing a hotel in a selected area is allowed
        // provided the nights are fully covered.
        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "test-prop-1",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            }
        };

        var result = await service.PlaceApprovedProposalHoldAsync(dto, 20);
        Assert.Equal(CheckoutStatus.Active, result.Status);
    }

    [Fact]
    public async Task AgenticHold_DateGaps_Throws()
    {
        var db = CreateDb(nameof(AgenticHold_DateGaps_Throws));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "test-prop-2",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 },
                // Gap between 1,4 and 1,5
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 5), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.PlaceApprovedProposalHoldAsync(dto, 20));
        Assert.Contains("contiguous with no gaps", ex.Message);
    }

    [Fact]
    public async Task AgenticHold_OneDayTrip_ZeroHotelsAllowed()
    {
        var db = CreateDb(nameof(AgenticHold_OneDayTrip_ZeroHotelsAllowed));
        var service = BuildService(db);

        db.Trips.Add(new Trip
        {
            Id = 2, TravelerId = 20, StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), Status = TripStatus.Planned, Budget = 100000m
        });
        db.ItineraryItems.AddRange(
            new ItineraryItem { Id = 3, TripId = 2, DestinationId = 1, DayNumber = 1 }
        );
        await db.SaveChangesAsync();

        var dto = new CreateCheckoutDto
        {
            TripId = 2,
            ProposalId = "test-prop-oneday",
            Hotels = new List<HotelCheckoutItemDto>(), // Empty hotels for a 1-day trip is valid!
            Vehicle = new VehicleCheckoutItemDto { VehicleId = 1, StartDate = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 1, 18, 0, 0, DateTimeKind.Utc) }
        };

        var result = await service.PlaceApprovedProposalHoldAsync(dto, 20);
        Assert.Equal(CheckoutStatus.Active, result.Status);
        Assert.Empty(result.Hotels);
    }

    [Fact]
    public async Task AgenticHold_RetrySameProposal_ReturnsOriginal()
    {
        var db = CreateDb(nameof(AgenticHold_RetrySameProposal_ReturnsOriginal));
        var service = BuildService(db);

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-retry-123",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            }
        };

        var firstResult = await service.PlaceApprovedProposalHoldAsync(dto, 20);
        
        // Expire the checkout to ensure it returns the same even if expired
        var checkout = await db.TripCheckouts.FindAsync(firstResult.Id);
        checkout!.Status = CheckoutStatus.Expired;
        await db.SaveChangesAsync();

        var secondResult = await service.PlaceApprovedProposalHoldAsync(dto, 20);
        
        // Must return the exact same checkout ID, not a new one
        Assert.Equal(firstResult.Id, secondResult.Id);
        Assert.Equal(CheckoutStatus.Expired, secondResult.Status);
    }

    [Fact]
    public async Task AgenticHold_RevisedProposal_CreatesNewHold()
    {
        var db = CreateDb(nameof(AgenticHold_RevisedProposal_CreatesNewHold));
        var service = BuildService(db);

        var dto1 = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-rev-A",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            }
        };
        var result1 = await service.PlaceApprovedProposalHoldAsync(dto1, 20);

        var dto2 = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-rev-B", // Different ID = revised proposal
            Hotels = new List<HotelCheckoutItemDto>
            {
                // Maybe they changed room or just different ID
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            }
        };
        var result2 = await service.PlaceApprovedProposalHoldAsync(dto2, 20);

        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task AgenticHold_SameProposalId_DifferentContent_Throws()
    {
        var db = CreateDb(nameof(AgenticHold_SameProposalId_DifferentContent_Throws));
        var service = BuildService(db);

        var dto1 = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-conflict",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 4), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 2, CheckInDate = new DateTime(2026, 1, 4), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            }
        };
        await service.PlaceApprovedProposalHoldAsync(dto1, 20);

        // Same proposal ID, but different trip ID or different content
        var dto2 = new CreateCheckoutDto
        {
            TripId = 1, // Same trip, different dates
            ProposalId = "prop-conflict",
            Hotels = new List<HotelCheckoutItemDto>
            {
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 2, 1), CheckOutDate = new DateTime(2026, 2, 5), NumberOfRooms = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.PlaceApprovedProposalHoldAsync(dto2, 20));
        Assert.Contains("contains different items or dates", ex.Message);
    }

    [Fact]
    public async Task AgenticHold_MixedRooms_Success()
    {
        var db = CreateDb(nameof(AgenticHold_MixedRooms_Success));
        var service = BuildService(db);

        // Kandy Hotel (Id=1) has Room 1 (added in CreateDb). Let's add another room (Room 4) to it.
        db.Rooms.Add(new Room { Id = 4, HotelId = 1, RoomType = "Triple", PricePerNight = 8000m, Capacity = 3, TotalRooms = 1, Status = RoomStatus.Active });
        await db.SaveChangesAsync();

        var dto = new CreateCheckoutDto
        {
            TripId = 1,
            ProposalId = "prop-mixed",
            Hotels = new List<HotelCheckoutItemDto>
            {
                // Both rooms are at Hotel 1 for the same dates
                new HotelCheckoutItemDto { RoomId = 1, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 },
                new HotelCheckoutItemDto { RoomId = 4, CheckInDate = new DateTime(2026, 1, 1), CheckOutDate = new DateTime(2026, 1, 10), NumberOfRooms = 1 }
            }
        };

        var result = await service.PlaceApprovedProposalHoldAsync(dto, 20);
        
        Assert.Equal(CheckoutStatus.Active, result.Status);
        Assert.Equal(2, result.Hotels.Count);
        // Total Price: 9 nights * (5000 + 8000) = 9 * 13000 = 117000
        Assert.Equal(117000m, result.TotalPrice);
    }
}
