import sys

with open('server/TourManagement.Api.Tests/Services/VehicleServiceTests.cs', 'r') as f:
    content = f.read()

new_tests = """    // ────────────────────────────────────────────────────────────────────────
    // Test 6: My Vehicles Bookings shows bookings for all owned vehicles
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyVehiclesBookings_ShowsBookingsForProvidersVehiclesOnly()
    {
        var db = CreateDb(nameof(GetMyVehiclesBookings_ShowsBookingsForProvidersVehiclesOnly));
        
        // Add another vehicle for provider 1
        db.Vehicles.Add(new Vehicle
        {
            Id                 = 2,
            ProviderId         = 1,
            VehicleType        = "Car",
            Model              = "Prius",
            RegistrationNumber = "WP-CA-1234",
            Capacity           = 4,
            PricePerDay        = 4000m,
            Status             = VehicleStatus.Active
        });

        // Add a vehicle for another provider (provider 3)
        db.Users.Add(new User { Id = 3, FullName = "Other", Email = "other@test.com", PasswordHash = "hash", Role = "TransportProvider" });
        db.Vehicles.Add(new Vehicle
        {
            Id                 = 3,
            ProviderId         = 3,
            VehicleType        = "Bus",
            Model              = "Rosa",
            RegistrationNumber = "WP-BU-1234",
            Capacity           = 30,
            PricePerDay        = 15000m,
            Status             = VehicleStatus.Active
        });

        // Add bookings for all 3 vehicles
        db.VehicleBookings.Add(new VehicleBooking { Id = 1, TripId = 1, VehicleId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Status = BookingStatus.Confirmed });
        db.VehicleBookings.Add(new VehicleBooking { Id = 2, TripId = 1, VehicleId = 2, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Status = BookingStatus.Confirmed });
        db.VehicleBookings.Add(new VehicleBooking { Id = 3, TripId = 1, VehicleId = 3, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Status = BookingStatus.Confirmed });
        await db.SaveChangesAsync();

        var vehicleService = new VehicleService(db);
        var bookingService = new VehicleBookingService(db, vehicleService);

        // Act: provider 1 asks for their vehicles' bookings
        var result = await bookingService.GetMyVehiclesBookingsAsync(1, null, null, 1, 10);

        // Assert: they should see 2 bookings (for vehicle 1 and 2), not the booking for vehicle 3
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, b => b.VehicleId == 1);
        Assert.Contains(result.Items, b => b.VehicleId == 2);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 7: IsBookedToday indicator computes correctly
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyVehicles_ComputesIsBookedTodayCorrectly()
    {
        var db = CreateDb(nameof(GetMyVehicles_ComputesIsBookedTodayCorrectly));
        
        // Add another vehicle for provider 1
        db.Vehicles.Add(new Vehicle
        {
            Id                 = 2,
            ProviderId         = 1,
            VehicleType        = "Car",
            Model              = "Prius",
            RegistrationNumber = "WP-CA-1234",
            Capacity           = 4,
            PricePerDay        = 4000m,
            Status             = VehicleStatus.Active
        });

        // Vehicle 1 has a booking covering today.
        db.VehicleBookings.Add(new VehicleBooking { TripId = 1, VehicleId = 1, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1), Status = BookingStatus.Confirmed });
        
        // Vehicle 2 has a booking in the future.
        db.VehicleBookings.Add(new VehicleBooking { TripId = 1, VehicleId = 2, StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(12), Status = BookingStatus.Confirmed });
        
        await db.SaveChangesAsync();

        var vehicleService = new VehicleService(db);

        // Act
        var result = await vehicleService.GetMyVehiclesAsync(1, null, null, null, 1, 10);

        // Assert
        var vehicle1 = result.Items.First(v => v.Id == 1);
        var vehicle2 = result.Items.First(v => v.Id == 2);

        Assert.True(vehicle1.IsBookedToday, "Vehicle 1 should be booked today.");
        Assert.False(vehicle2.IsBookedToday, "Vehicle 2 should NOT be booked today.");
    }
}"""

content = content.replace("}\n", new_tests + "\n", 1)

with open('server/TourManagement.Api.Tests/Services/VehicleServiceTests.cs', 'w') as f:
    f.write(content)
