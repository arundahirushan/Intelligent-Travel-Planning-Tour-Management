using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Accommodation;

// Lightweight view used in list responses (e.g. "my hotels", admin list).
public class HotelSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public int? StarRating { get; set; }
    public HotelStatus Status { get; set; }

    // OwnerName is mainly useful in the Admin's view.
    public string OwnerName { get; set; } = string.Empty;
}
