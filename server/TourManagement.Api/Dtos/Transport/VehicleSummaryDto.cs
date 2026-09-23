using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Transport;

// Lightweight view used in list responses (e.g. "my vehicles", admin list).
public class VehicleSummaryDto
{
    public int Id { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PricePerDay { get; set; }
    public VehicleStatus Status { get; set; }

    // ProviderName is mainly useful in the Admin's view.
    public string ProviderName { get; set; } = string.Empty;

    // True if there is a Held or Confirmed booking for today.
    public bool IsBookedToday { get; set; }
}

