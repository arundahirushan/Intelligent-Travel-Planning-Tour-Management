using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using TourManagement.Api.Services.Interfaces;
using TourManagement.Api.AgentIntegration;
using TourManagement.Api.Common.Exceptions;
using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Tests.Services;

public class FakeAgentClient : IAgentServiceClient
{
    public AgentProposalResult NextResult { get; set; } = new();
    
    public Task<AgentProposalResult> GenerateProposalAsync(string proposalId, int tripId, string requestSnapshotJson)
    {
        return Task.FromResult(NextResult);
    }
}

public class WorkflowServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly WorkflowService _workflowService;
    private readonly FakeAgentClient _agentClient;

    public WorkflowServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);

        var hotelService = new HotelService(_db);
        var vehicleService = new VehicleService(_db);
        var checkoutService = new CheckoutService(_db, hotelService, vehicleService);
        
        _agentClient = new FakeAgentClient();
        _workflowService = new WorkflowService(_db, _agentClient, checkoutService, Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowService>.Instance);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task<Trip> SetupTripAsync()
    {
        var trip = new Trip
        {
            TravelerId = 1,
            Title = "Test Trip",
            StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            Budget = 5000,
            GroupSize = 2,
            Status = TripStatus.Draft
        };
        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();
        return trip;
    }

    [Fact]
    public async Task GenerateProposal_M3Fails_StatusIsGenerationFailed_PayloadSurvives()
    {
        // Arrange
        var trip = await SetupTripAsync();
        var payloadJson = @"
        {
            ""Hotels"": [
                { ""RoomId"": 101, ""CheckInDate"": ""2026-10-01T00:00:00Z"", ""CheckOutDate"": ""2026-10-05T00:00:00Z"", ""NumberOfRooms"": 2 }
            ],
            ""Vehicle"": null,
            ""PartialPlan"": {
                ""Days"": [
                    { ""Date"": ""2026-10-01T00:00:00Z"", ""DestinationId"": 1, ""Activities"": [""Beach""] }
                ]
            },
            ""PartialAccommodation"": {
                ""Summary"": ""Stay at Sea View"",
                ""Hotels"": [ { ""HotelId"": 1, ""Name"": ""Sea View"" } ]
            },
            ""PartialTransport"": null,
            ""PartialWeather"": null
        }";
        
        _agentClient.NextResult = new AgentProposalResult
        {
            Status = "GenerationFailed",
            Payload = payloadJson,
            ExecutionSummaries = new List<AgentExecutionSummary>
            {
                new() { AgentIdentity = "m1_planning", FinalOutcome = "Pass" },
                new() { AgentIdentity = "m2_accommodation", FinalOutcome = "Pass" },
                new() { AgentIdentity = "m3_transport_weather", FinalOutcome = "Failed", Errors = "[\"No drivers available\"]" }
            }
        };

        // Act
        var result = await _workflowService.GenerateProposalAsync(trip.Id, travelerId: 1);

        // Assert
        Assert.Equal("GenerationFailed", result.Status);
        
        // Retrieve directly via WorkflowService to verify storage
        var retrieved = await _workflowService.GetLatestProposalAsync(trip.Id, travelerId: 1);
        Assert.Equal("GenerationFailed", retrieved.Status);
        
        var payloadDoc = JsonSerializer.Deserialize<JsonElement>(retrieved.Payload?.ToString() ?? "{}");
        
        // Assert M1 Plan dates and destination details
        var planDay = payloadDoc.GetProperty("PartialPlan").GetProperty("Days")[0];
        Assert.Equal("2026-10-01T00:00:00Z", planDay.GetProperty("Date").GetString());
        Assert.Equal(1, planDay.GetProperty("DestinationId").GetInt32());
        
        // Assert M2 Room IDs, quantities and accommodation summary
        var roomNode = payloadDoc.GetProperty("Hotels")[0];
        Assert.Equal(101, roomNode.GetProperty("RoomId").GetInt32());
        Assert.Equal(2, roomNode.GetProperty("NumberOfRooms").GetInt32());
        Assert.Equal("Stay at Sea View", payloadDoc.GetProperty("PartialAccommodation").GetProperty("Summary").GetString());
        
        // Assert Failure status, failed step, and reason
        Assert.Contains(retrieved.ExecutionSummaries, s => 
            s.AgentIdentity == "m3_transport_weather" && 
            s.FinalOutcome == "Failed" && 
            (s.Errors?.ToString()?.Contains("No drivers available") == true));
    }

    [Fact]
    public async Task GenerateProposal_M4Unimplemented_CannotBeAcceptedOrApproved()
    {
        // Arrange
        var trip = await SetupTripAsync();
        var payloadJson = @"
        {
            ""Hotels"": [
                { ""RoomId"": 101, ""CheckInDate"": ""2026-10-01T00:00:00Z"", ""CheckOutDate"": ""2026-10-05T00:00:00Z"", ""NumberOfRooms"": 2 }
            ],
            ""Vehicle"": {
                ""VehicleId"": 201,
                ""StartDate"": ""2026-10-01T00:00:00Z"",
                ""EndDate"": ""2026-10-05T00:00:00Z"",
                ""PickupLatitude"": 6.9,
                ""PickupLongitude"": 79.8,
                ""PickupNote"": ""Airport""
            },
            ""PartialPlan"": {
                ""Days"": [
                    { ""Date"": ""2026-10-01T00:00:00Z"", ""DestinationId"": 1, ""Activities"": [""Beach""] }
                ]
            },
            ""PartialAccommodation"": {
                ""Summary"": ""Stay at Sea View""
            },
            ""PartialTransport"": {
                ""TransportCost"": 5000,
                ""Currency"": ""LKR"",
                ""RemainingBudget"": 30000,
                ""CombinedTotal"": 20000
            },
            ""PartialWeather"": {
                ""Condition"": ""Sunny"",
                ""DestinationId"": 1,
                ""Date"": ""2026-10-01T00:00:00Z"",
                ""Advisory"": ""Wear sunscreen""
            }
        }";
        
        _agentClient.NextResult = new AgentProposalResult
        {
            Status = "Generated",
            Payload = payloadJson,
            ExecutionSummaries = new List<AgentExecutionSummary>
            {
                new() { AgentIdentity = "m1_planning", FinalOutcome = "Pass" },
                new() { AgentIdentity = "m2_accommodation", FinalOutcome = "Pass" },
                new() { AgentIdentity = "m3_transport_weather", FinalOutcome = "Pass" },
                new() { AgentIdentity = "m4_validation", FinalOutcome = "NotImplemented" }
            }
        };

        // Act 1: Generate
        var proposal = await _workflowService.GenerateProposalAsync(trip.Id, travelerId: 1);
        Assert.Equal("Generated", proposal.Status);
        
        // Verify payload persistence for M1/M2/M3 completion
        var retrieved = await _workflowService.GetLatestProposalAsync(trip.Id, travelerId: 1);
        var payloadDoc = JsonSerializer.Deserialize<JsonElement>(retrieved.Payload?.ToString() ?? "{}");
        
        // Assert M3 vehicle ID, rental dates and pickup coordinates
        var vehicle = payloadDoc.GetProperty("Vehicle");
        Assert.Equal(201, vehicle.GetProperty("VehicleId").GetInt32());
        Assert.Equal("2026-10-01T00:00:00Z", vehicle.GetProperty("StartDate").GetString());
        Assert.Equal(6.9m, vehicle.GetProperty("PickupLatitude").GetDecimal());
        Assert.Equal(79.8m, vehicle.GetProperty("PickupLongitude").GetDecimal());
        
        // Assert Transport cost, combined total, remaining budget and currency
        var transport = payloadDoc.GetProperty("PartialTransport");
        Assert.Equal(5000, transport.GetProperty("TransportCost").GetDecimal());
        Assert.Equal("LKR", transport.GetProperty("Currency").GetString());
        Assert.Equal(30000, transport.GetProperty("RemainingBudget").GetDecimal());
        Assert.Equal(20000, transport.GetProperty("CombinedTotal").GetDecimal());
        
        // Assert Weather destination/date, status and advisory data
        var weather = payloadDoc.GetProperty("PartialWeather");
        Assert.Equal("Sunny", weather.GetProperty("Condition").GetString());
        Assert.Equal(1, weather.GetProperty("DestinationId").GetInt32());
        Assert.Equal("2026-10-01T00:00:00Z", weather.GetProperty("Date").GetString());
        Assert.Equal("Wear sunscreen", weather.GetProperty("Advisory").GetString());
        
        // Assert Unfinished step
        Assert.Contains(retrieved.ExecutionSummaries, s => 
            s.AgentIdentity == "m4_validation" && 
            s.FinalOutcome == "NotImplemented");

        // Act 2 & Assert: Attempt Accept -> Fails
        await Assert.ThrowsAsync<ValidationException>(() => 
            _workflowService.AcceptProposalAsync(trip.Id, retrieved.ProposalId, travelerId: 1));

        // Act 3 & Assert: Attempt Approve -> Fails because status won't be PendingAdminApproval
        await Assert.ThrowsAsync<ValidationException>(() => 
            _workflowService.ApproveProposalAsync(retrieved.ProposalId, adminId: 99));
            
        // Verify no checkouts/bookings were created
        var checkouts = await _db.TripCheckouts.CountAsync();
        Assert.Equal(0, checkouts);
    }
}
