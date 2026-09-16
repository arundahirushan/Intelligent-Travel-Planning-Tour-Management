using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Trips;

namespace TourManagement.Api.Services.Interfaces;

public interface ITripService
{
    // Traveler: create a new trip (starts as Draft).
    Task<TripDetailDto> CreateAsync(CreateTripDto dto, int travelerId);

    // Traveler: list their own trips.
    Task<PagedResult<TripSummaryDto>> GetMyTripsAsync(
        int travelerId, string? status, string? search, string? sort, int page, int pageSize);

    // Traveler or Admin: get full trip detail including itinerary.
    // requestingUserId and requestingUserRole are used to check ownership.
    Task<TripDetailDto> GetByIdAsync(int id, int requestingUserId, string requestingUserRole);

    // Traveler: update a trip (only while Status = Draft).
    Task<TripDetailDto> UpdateAsync(int id, UpdateTripDto dto, int requestingUserId);

    // Traveler: cancel a trip (only if Draft or Planned).
    Task CancelAsync(int id, int requestingUserId);

    // Admin override: force-cancel any trip regardless of status.
    Task ForceCancelAsync(int id);

    // Admin: list all trips across all travelers.
    Task<PagedResult<TripSummaryDto>> GetAllAsync(
        string? status, int? destinationId, int? travelerId,
        string? search, string? sort, int page, int pageSize);

    // Itinerary item management.
    Task<TripDetailDto> AddItineraryItemAsync(int tripId, CreateItineraryItemDto dto, int requestingUserId);
    Task<TripDetailDto> UpdateItineraryItemAsync(int tripId, int itemId, UpdateItineraryItemDto dto, int requestingUserId);
    Task<TripDetailDto> RemoveItineraryItemAsync(int tripId, int itemId, int requestingUserId);

    // Business operation: auto-generate a starting itinerary by splitting days across destinations.
    Task<TripDetailDto> GenerateDraftItineraryAsync(int tripId, GenerateDraftItineraryDto dto, int requestingUserId);
}
