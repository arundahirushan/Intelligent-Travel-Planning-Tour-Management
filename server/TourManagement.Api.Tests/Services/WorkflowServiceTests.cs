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
        _workflowService = new WorkflowService(_db, _agentClient, checkoutService);
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
        var payloadJson = "{\"PartialPlan\":{\"Days\":[]},\"PartialAccommodation\":{\"Hotels\":[]},\"PartialTransport\":null,\"PartialWeather\":null}";
        
        _agentClient.NextResult = new AgentProposalResult
        {
            Status = "GenerationFailed",
            Payload = payloadJson,
            ExecutionSummaries = new List<AgentExecutionSummary>
            {
                new() { AgentIdentity = "m1_planning", FinalOutcome = "Pass" },
                new() { AgentIdentity = "m2_accommodation", FinalOutcome = "Pass" },
                new() { AgentIdentity = "m3_transport_weather", FinalOutcome = "Failed", Errors = "[\"API timeout\"]" }
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
        Assert.True(payloadDoc.TryGetProperty("PartialPlan", out _));
        Assert.True(payloadDoc.TryGetProperty("PartialAccommodation", out _));
        Assert.Contains(retrieved.ExecutionSummaries, s => s.AgentIdentity == "m3_transport_weather" && s.FinalOutcome == "Failed");
    }

    [Fact]
    public async Task GenerateProposal_M4Unimplemented_CannotBeAcceptedOrApproved()
    {
        // Arrange
        var trip = await SetupTripAsync();
        var payloadJson = "{\"Hotels\":[],\"Vehicle\":null,\"PartialPlan\":{\"Days\":[]},\"PartialAccommodation\":{\"Hotels\":[]},\"PartialTransport\":{\"Vehicles\":[]},\"PartialWeather\":{\"Condition\":\"Sunny\"}}";
        
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
        
        // Verify payload persistence
        var retrieved = await _workflowService.GetLatestProposalAsync(trip.Id, travelerId: 1);
        var payloadDoc = JsonSerializer.Deserialize<JsonElement>(retrieved.Payload?.ToString() ?? "{}");
        Assert.True(payloadDoc.TryGetProperty("PartialWeather", out var weather));
        Assert.Equal("Sunny", weather.GetProperty("Condition").GetString());

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
