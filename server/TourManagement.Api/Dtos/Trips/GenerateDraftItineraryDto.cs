using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Trips;

// Input for the generate-draft-itinerary endpoint.
// The traveler provides an ordered list of destination IDs they want to visit.
// The service splits the trip's days evenly across these destinations.
public class GenerateDraftItineraryDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one destination is required.")]
    public List<int> DestinationIds { get; set; } = new();
}
