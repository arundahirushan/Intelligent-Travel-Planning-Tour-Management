namespace TourManagement.Api.Dtos.Workflows;

// ── Request sent by M4 to POST /api/internal/proposals/validate ─────────────
// The trip and proposal identity are resolved server-side from X-AI-ProposalId;
// M4 sends only the candidate selections and itinerary details.

public class ValidateProposalRequestDto
{
    // Hotel room selections proposed by M2.
    public List<ValidationHotelItemDto> Hotels { get; set; } = new();

    // Single vehicle proposed by M3. Null for a valid one-day trip with no vehicle.
    public ValidationVehicleItemDto? Vehicle { get; set; }

    // M1 overnight sections — needed to verify hotel/section agreement.
    public List<ValidationOvernightSectionDto> OvernightSections { get; set; } = new();
}

public class ValidationHotelItemDto
{
    public int RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    // Number of rooms of this type in this stay.
    public int NumberOfRooms { get; set; }
}

public class ValidationVehicleItemDto
{
    public int VehicleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    // Traveler-provided pickup from the generation snapshot.
    public decimal PickupLatitude { get; set; }
    public decimal PickupLongitude { get; set; }
    public string? PickupNote { get; set; }
}

/// <summary>M1 overnight section sent for itinerary-hotel agreement checks.</summary>
public class ValidationOvernightSectionDto
{
    public int OvernightAreaId { get; set; }
    public string CheckInDate { get; set; } = string.Empty;
    public string CheckOutDate { get; set; } = string.Empty;
}

// ── Response returned to M4 ──────────────────────────────────────────────────

public class ValidateProposalResultDto
{
    // True only when all authoritative checks pass.
    public bool IsValid { get; set; }

    // Stable machine-readable codes, e.g. "CAPACITY_INSUFFICIENT".
    public List<string> IssueCodes { get; set; } = new();

    // Human-readable parallel to IssueCodes.
    public List<string> Issues { get; set; } = new();

    // Non-blocking advisory notes (e.g. availability margin, price warnings).
    public List<string> Warnings { get; set; } = new();

    // Authoritative cost breakdown from the database.
    public decimal AccommodationCost { get; set; }
    public decimal TransportCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal Budget { get; set; }
    public string Currency { get; set; } = "LKR";

    // Set when the saved generation inputs differ from current trip data.
    public bool StaleInputDetected { get; set; }
}
