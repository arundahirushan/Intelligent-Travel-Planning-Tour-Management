using TourManagement.Api.Dtos.Trips;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert ItineraryItem entities to DTOs.
public static class ItineraryItemMappings
{
    public static ItineraryItemDto ToDto(this ItineraryItem item)
    {
        return new ItineraryItemDto
        {
            Id              = item.Id,
            DestinationId   = item.DestinationId,
            // Pull the name from the navigation property. If the query
            // didn't include() Destination, this will throw — always
            // Include(i => i.Destination) when fetching items for a response.
            DestinationName = item.Destination?.Name ?? string.Empty,
            DayNumber       = item.DayNumber,
            Notes           = item.Notes,
            SequenceOrder   = item.SequenceOrder
        };
    }
}
