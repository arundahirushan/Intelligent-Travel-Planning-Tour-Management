using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Xunit;
using TourManagement.Api.Controllers;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Workflows;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using TourManagement.Api.Common;

namespace TourManagement.Api.Tests.Controllers;

/// <summary>
/// Tests for InternalAgentController.ValidateProposal (POST /api/internal/proposals/validate).
/// All checks are read-only — no holds or bookings are created.
/// The DB is seeded fresh per test via an in-memory database.
/// </summary>
public class InternalAgentControllerValidationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly InternalAgentController _controller;

    private const string TestSecret = "test-ai-secret";
    private const string TestProposalId = "test-proposal-abc";

    public InternalAgentControllerValidationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);

        var hotelService   = new HotelService(_db);
        var vehicleService = new VehicleService(_db);
        var tripService    = new TripService(_db);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiServiceSettings:Secret"] = TestSecret
            })
            .Build();

        _controller = new InternalAgentController(tripService, hotelService, vehicleService, _db, config);

        // Attach headers so GetAuthorizedProposalAsync passes.
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-AI-Secret"]     = TestSecret;
        httpContext.Request.Headers["X-AI-ProposalId"] = TestProposalId;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // ── Helper: unwrap ActionResult<ApiResponse<T>> → ApiResponse<T> ─────────

    private static ApiResponse<ValidateProposalResultDto> Unwrap(
        ActionResult<ApiResponse<ValidateProposalResultDto>> actionResult)
    {
        var ok = actionResult.Result as OkObjectResult;
        Assert.NotNull(ok);
        var response = ok!.Value as ApiResponse<ValidateProposalResultDto>;
        Assert.NotNull(response);
        return response!;
    }

    // ── Seed helpers ─────────────────────────────────────────────────────────

    private async Task<Trip> SeedTripAsync(int groupSize = 2, decimal budget = 50000m, bool oneDayTrip = false)
    {
        var dest = new Destination { Name = "Kandy", Description = "Test" };
        _db.Destinations.Add(dest);
        await _db.SaveChangesAsync();

        var trip = new Trip
        {
            TravelerId = 1,
            Title      = "Test Trip",
            StartDate  = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate    = oneDayTrip
                ? new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
                : new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
            Budget    = budget,
            GroupSize = groupSize,
            Status    = TripStatus.Draft
        };
        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        // ItineraryItem (not TripItineraryItem) is the actual model name.
        var itinerary = new ItineraryItem { TripId = trip.Id, DestinationId = dest.Id, DayNumber = 1 };
        _db.ItineraryItems.Add(itinerary);

        var snapshot = JsonSerializer.Serialize(new
        {
            GroupSize = groupSize,
            Budget    = budget,
            StartDate = trip.StartDate.ToString("o"),
            EndDate   = trip.EndDate.ToString("o"),
            PickupLatitude  = 6.9271m,
            PickupLongitude = 79.8612m,
        });

        var proposal = new TripProposal
        {
            ProposalId    = TestProposalId,
            TripId        = trip.Id,
            Status        = ProposalStatus.Generating,
            InputSnapshot = snapshot,
            Payload       = "{}",
            Version       = 1,
            RequestId     = Guid.NewGuid().ToString(),
        };
        _db.TripProposals.Add(proposal);
        await _db.SaveChangesAsync();

        return trip;
    }

    private async Task<(Hotel hotel, Room room)> SeedHotelRoomAsync(
        int destinationId, int totalRooms = 5, int capacity = 2, decimal pricePerNight = 5000m)
    {
        // User.Role is a plain string — use the Roles constants string values.
        var owner = new User
        {
            Email        = $"owner-{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role         = "HotelOwner",
        };
        _db.Users.Add(owner);
        await _db.SaveChangesAsync();

        var hotel = new Hotel
        {
            OwnerId       = owner.Id,
            DestinationId = destinationId,
            Name          = "Test Hotel",
            Status        = HotelStatus.Active,
        };
        _db.Hotels.Add(hotel);
        await _db.SaveChangesAsync();

        var room = new Room
        {
            HotelId       = hotel.Id,
            RoomType      = "Double",
            Capacity      = capacity,
            TotalRooms    = totalRooms,
            PricePerNight = pricePerNight,
            Status        = RoomStatus.Active,
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return (hotel, room);
    }

    private async Task<Vehicle> SeedVehicleAsync(int capacity = 4, decimal pricePerDay = 5000m)
    {
        var provider = new User
        {
            Email        = $"provider-{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role         = "TransportProvider",
        };
        _db.Users.Add(provider);
        await _db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            ProviderId         = provider.Id,
            VehicleType        = "Van",
            Model              = "KDH",
            RegistrationNumber = $"ABC-{Guid.NewGuid():N}"[..8],
            Capacity           = capacity,
            PricePerDay        = pricePerDay,
            Status             = VehicleStatus.Active,
        };
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return vehicle;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_HappyPath_ReturnsIsValid()
    {
        var trip = await SeedTripAsync(groupSize: 2, budget: 50000m);
        var dest = await _db.Destinations.FirstAsync();
        var (_, room) = await SeedHotelRoomAsync(dest.Id, totalRooms: 5, capacity: 2, pricePerNight: 5000m);
        var vehicle   = await SeedVehicleAsync(capacity: 4, pricePerDay: 5000m);

        var request = new ValidateProposalRequestDto
        {
            Hotels = new List<ValidationHotelItemDto>
            {
                new()
                {
                    RoomId        = room.Id,
                    CheckInDate   = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CheckOutDate  = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                    NumberOfRooms = 1,
                }
            },
            Vehicle = new ValidationVehicleItemDto
            {
                VehicleId       = vehicle.Id,
                StartDate       = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate         = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                PickupLatitude  = 6.9271m,
                PickupLongitude = 79.8612m,
            },
            OvernightSections = new List<ValidationOvernightSectionDto>
            {
                new() { OvernightAreaId = dest.Id, CheckInDate = "2026-10-01", CheckOutDate = "2026-10-03" }
            }
        };

        var response = Unwrap(await _controller.ValidateProposal(request));
        Assert.True(response.Success);
        Assert.True(response.Data!.IsValid);
        Assert.Empty(response.Data.IssueCodes);
        Assert.Equal(10000m, response.Data.AccommodationCost);  // 5000 * 1 room * 2 nights
        Assert.Equal(10000m, response.Data.TransportCost);       // 5000 * 2 days
    }

    [Fact]
    public async Task Validate_CapacityInsufficient_ReturnsIssue()
    {
        // Group of 4 but only 1 double room (capacity 2, 1 room booked).
        var trip = await SeedTripAsync(groupSize: 4, budget: 100000m);
        var dest = await _db.Destinations.FirstAsync();
        var (_, room) = await SeedHotelRoomAsync(dest.Id, totalRooms: 5, capacity: 2);
        var vehicle   = await SeedVehicleAsync(capacity: 6);

        var request = new ValidateProposalRequestDto
        {
            Hotels = new List<ValidationHotelItemDto>
            {
                new()
                {
                    RoomId        = room.Id,
                    CheckInDate   = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CheckOutDate  = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                    NumberOfRooms = 1,  // 1 room × 2 capacity = 2 guests < 4 required
                }
            },
            Vehicle = new ValidationVehicleItemDto
            {
                VehicleId       = vehicle.Id,
                StartDate       = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate         = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                PickupLatitude  = 6.9271m,
                PickupLongitude = 79.8612m,
            },
        };

        var response = Unwrap(await _controller.ValidateProposal(request));
        Assert.False(response.Data!.IsValid);
        Assert.Contains("CAPACITY_INSUFFICIENT", response.Data.IssueCodes);
    }

    [Fact]
    public async Task Validate_InvalidVehicleId_ReturnsIssue()
    {
        var trip = await SeedTripAsync();
        var dest = await _db.Destinations.FirstAsync();
        var (_, room) = await SeedHotelRoomAsync(dest.Id, capacity: 4);

        var request = new ValidateProposalRequestDto
        {
            Hotels = new List<ValidationHotelItemDto>
            {
                new()
                {
                    RoomId        = room.Id,
                    CheckInDate   = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CheckOutDate  = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                    NumberOfRooms = 1,
                }
            },
            Vehicle = new ValidationVehicleItemDto
            {
                VehicleId       = 99999,  // Does not exist
                StartDate       = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate         = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                PickupLatitude  = 6.9271m,
                PickupLongitude = 79.8612m,
            },
        };

        var response = Unwrap(await _controller.ValidateProposal(request));
        Assert.False(response.Data!.IsValid);
        Assert.Contains("INVALID_VEHICLE_ID", response.Data.IssueCodes);
    }

    [Fact]
    public async Task Validate_OverBudget_ReturnsIssue()
    {
        // Budget = 5000 LKR, but accommodation alone = 10000 LKR (5000/night × 2 nights).
        var trip = await SeedTripAsync(groupSize: 2, budget: 5000m);
        var dest = await _db.Destinations.FirstAsync();
        var (_, room) = await SeedHotelRoomAsync(dest.Id, capacity: 2, pricePerNight: 5000m);
        var vehicle   = await SeedVehicleAsync(capacity: 4, pricePerDay: 1000m);

        var request = new ValidateProposalRequestDto
        {
            Hotels = new List<ValidationHotelItemDto>
            {
                new()
                {
                    RoomId        = room.Id,
                    CheckInDate   = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CheckOutDate  = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                    NumberOfRooms = 1,
                }
            },
            Vehicle = new ValidationVehicleItemDto
            {
                VehicleId       = vehicle.Id,
                StartDate       = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate         = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                PickupLatitude  = 6.9271m,
                PickupLongitude = 79.8612m,
            },
        };

        var response = Unwrap(await _controller.ValidateProposal(request));
        Assert.False(response.Data!.IsValid);
        Assert.Contains("OVER_BUDGET", response.Data.IssueCodes);
    }

    [Fact]
    public async Task Validate_MissingVehicle_ReturnsIssue()
    {
        var trip = await SeedTripAsync();
        var dest = await _db.Destinations.FirstAsync();
        var (_, room) = await SeedHotelRoomAsync(dest.Id, capacity: 4);

        var request = new ValidateProposalRequestDto
        {
            Hotels = new List<ValidationHotelItemDto>
            {
                new()
                {
                    RoomId        = room.Id,
                    CheckInDate   = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CheckOutDate  = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                    NumberOfRooms = 2,
                }
            },
            Vehicle = null,
        };

        var response = Unwrap(await _controller.ValidateProposal(request));
        Assert.False(response.Data!.IsValid);
        Assert.Contains("MISSING_VEHICLE", response.Data.IssueCodes);
    }

    [Fact]
    public async Task Validate_StaleGroupSize_SetsStaleFlag()
    {
        // Seed trip with groupSize=2, but the snapshot stored groupSize=4.
        var trip = await SeedTripAsync(groupSize: 2, budget: 100000m);

        // Overwrite the snapshot to simulate a changed groupSize after generation started.
        var proposal = await _db.TripProposals.FirstAsync(p => p.ProposalId == TestProposalId);
        var staleSnap = JsonSerializer.Serialize(new
        {
            GroupSize = 4,   // Different from trip.GroupSize = 2
            Budget    = 100000m,
            StartDate = "2026-10-01T00:00:00Z",
            EndDate   = "2026-10-03T00:00:00Z",
            PickupLatitude  = 6.9271m,
            PickupLongitude = 79.8612m,
        });
        proposal.InputSnapshot = staleSnap;
        await _db.SaveChangesAsync();

        var dest = await _db.Destinations.FirstAsync();
        var (_, room) = await SeedHotelRoomAsync(dest.Id, capacity: 4);
        var vehicle   = await SeedVehicleAsync(capacity: 4);

        var request = new ValidateProposalRequestDto
        {
            Hotels = new List<ValidationHotelItemDto>
            {
                new()
                {
                    RoomId        = room.Id,
                    CheckInDate   = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CheckOutDate  = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                    NumberOfRooms = 1,
                }
            },
            Vehicle = new ValidationVehicleItemDto
            {
                VehicleId       = vehicle.Id,
                StartDate       = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate         = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                PickupLatitude  = 6.9271m,
                PickupLongitude = 79.8612m,
            },
        };

        var response = Unwrap(await _controller.ValidateProposal(request));
        Assert.True(response.Data!.StaleInputDetected);
        Assert.Contains("STALE_INPUTS", response.Data.IssueCodes);
    }
}
