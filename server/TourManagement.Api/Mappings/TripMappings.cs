using TourManagement.Api.Dtos.Trips;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert Trip entities to DTOs.
public static class TripMappings
{
    // Lightweight summary used in list views.
    public static TripSummaryDto ToSummaryDto(this Trip trip)
    {
        return new TripSummaryDto
        {
            Id        = trip.Id,
            Title     = trip.Title,
            StartDate = trip.StartDate,
            EndDate   = trip.EndDate,
            Status    = trip.Status,
            Budget    = trip.Budget
        };
    }

    // Full detail including itinerary items.
    // Assumes trip.ItineraryItems and each item.Destination are already loaded.
    public static TripDetailDto ToDetailDto(this Trip trip)
    {
        return new TripDetailDto
        {
            Id        = trip.Id,
            TravelerId = trip.TravelerId,
            Title     = trip.Title,
            StartDate = trip.StartDate,
            EndDate   = trip.EndDate,
            Budget    = trip.Budget,
            GroupSize = trip.GroupSize,
            Interests = trip.Interests,
            Status    = trip.Status,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt,
            // Items are pre-sorted by DayNumber then SequenceOrder in the service query.
            ItineraryItems = trip.ItineraryItems
                .Select(i => i.ToDto())
                .ToList()
        };
    }
}
