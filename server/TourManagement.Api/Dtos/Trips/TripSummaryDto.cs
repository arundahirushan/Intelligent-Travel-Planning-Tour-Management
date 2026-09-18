using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Trips;

// A compact trip representation used in list views (my trips, admin trip list).
public class TripSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TripStatus Status { get; set; }
    public decimal Budget { get; set; }
}
