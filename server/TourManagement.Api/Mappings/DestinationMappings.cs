using TourManagement.Api.Dtos.Destination;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert Destination entities to/from DTOs.
public static class DestinationMappings
{
    public static DestinationResponseDto ToResponseDto(this Destination dest)
    {
        return new DestinationResponseDto
        {
            Id          = dest.Id,
            Name        = dest.Name,
            Region      = dest.Region,
            Description = dest.Description,
            ImageUrl    = dest.ImageUrl,
            CreatedAt   = dest.CreatedAt,
            UpdatedAt   = dest.UpdatedAt
        };
    }

    // Creates a new Destination entity from a CreateDestinationDto.
    public static Destination ToEntity(this CreateDestinationDto dto)
    {
        return new Destination
        {
            Name        = dto.Name,
            Region      = dto.Region,
            Description = dto.Description,
            ImageUrl    = dto.ImageUrl,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };
    }

    // Updates an existing Destination entity with values from an UpdateDestinationDto.
    public static void UpdateFromDto(this Destination dest, UpdateDestinationDto dto)
    {
        dest.Name        = dto.Name;
        dest.Region      = dto.Region;
        dest.Description = dto.Description;
        dest.ImageUrl    = dto.ImageUrl;
        dest.UpdatedAt   = DateTime.UtcNow;
    }
}
