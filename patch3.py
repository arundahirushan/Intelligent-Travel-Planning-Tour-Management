import sys

with open('server/TourManagement.Api/Controllers/VehicleBookingsController.cs', 'r') as f:
    content = f.read()

new_method = """    }

    /// <summary>Get all bookings across all vehicles owned by the TransportProvider.</summary>
    [HttpGet("my-vehicles")]
    [Authorize(Roles = Roles.TransportProvider)]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleBookingSummaryDto>>>> GetMyVehiclesBookings(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _bookingService.GetMyVehiclesBookingsAsync(GetCurrentUserId(), search, status, page, pageSize);
        return Ok(ApiResponse<PagedResult<VehicleBookingSummaryDto>>.Ok(result));
    }

    /// <summary>Cancel a vehicle booking. Trip owner or Admin/SuperAdmin only.</summary>"""

content = content.replace("""    }

    /// <summary>Cancel a vehicle booking. Trip owner or Admin/SuperAdmin only.</summary>""", new_method)

with open('server/TourManagement.Api/Controllers/VehicleBookingsController.cs', 'w') as f:
    f.write(content)
