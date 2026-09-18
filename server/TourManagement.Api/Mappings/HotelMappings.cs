using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert Hotel entities to/from DTOs.
public static class HotelMappings
{
    // Lightweight summary — assumes hotel.Owner and hotel.Destination are loaded.
    public static HotelSummaryDto ToSummaryDto(this Hotel hotel)
    {
        return new HotelSummaryDto
        {
            Id              = hotel.Id,
            Name            = hotel.Name,
            DestinationName = hotel.Destination?.Name ?? string.Empty,
            StarRating      = hotel.StarRating,
            Status          = hotel.Status,
            OwnerName       = hotel.Owner?.FullName ?? string.Empty
        };
    }

    // Full detail — assumes hotel.Owner, hotel.Destination, and hotel.Rooms are loaded.
    public static HotelDetailDto ToDetailDto(this Hotel hotel)
    {
        return new HotelDetailDto
        {
            Id              = hotel.Id,
            OwnerId         = hotel.OwnerId,
            OwnerName       = hotel.Owner?.FullName ?? string.Empty,
            DestinationId   = hotel.DestinationId,
            DestinationName = hotel.Destination?.Name ?? string.Empty,
            Name            = hotel.Name,
            Address         = hotel.Address,
            Description     = hotel.Description,
            ContactPhone    = hotel.ContactPhone,
            StarRating      = hotel.StarRating,
            ImageUrl        = hotel.ImageUrl,
            Status          = hotel.Status,
            CreatedAt       = hotel.CreatedAt,
            UpdatedAt       = hotel.UpdatedAt,
            Rooms           = hotel.Rooms.Select(r => r.ToDto()).ToList()
        };
    }

    // Creates a new Hotel entity from a CreateHotelDto.
    // OwnerId and Status are set by the service, not here.
    public static Hotel ToEntity(this CreateHotelDto dto, int ownerId)
    {
        return new Hotel
        {
            OwnerId       = ownerId,
            DestinationId = dto.DestinationId,
            Name          = dto.Name,
            Address       = dto.Address,
            Description   = dto.Description,
            ContactPhone  = dto.ContactPhone,
            StarRating    = dto.StarRating,
            ImageUrl      = dto.ImageUrl,
            Status        = HotelStatus.PendingApproval,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };
    }

    // Updates an existing Hotel entity with values from an UpdateHotelDto.
    public static void UpdateFromDto(this Hotel hotel, UpdateHotelDto dto)
    {
        hotel.Name          = dto.Name;
        hotel.DestinationId = dto.DestinationId;
        hotel.Address       = dto.Address;
        hotel.Description   = dto.Description;
        hotel.ContactPhone  = dto.ContactPhone;
        hotel.StarRating    = dto.StarRating;
        hotel.ImageUrl      = dto.ImageUrl;
        hotel.UpdatedAt     = DateTime.UtcNow;
    }
}
