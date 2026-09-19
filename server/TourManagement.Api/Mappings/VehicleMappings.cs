using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert Vehicle entities to/from DTOs.
public static class VehicleMappings
{
    // Lightweight summary — assumes vehicle.Provider is loaded.
    public static VehicleSummaryDto ToSummaryDto(this Vehicle vehicle)
    {
        return new VehicleSummaryDto
        {
            Id           = vehicle.Id,
            VehicleType  = vehicle.VehicleType,
            Model        = vehicle.Model,
            Capacity     = vehicle.Capacity,
            PricePerDay  = vehicle.PricePerDay,
            Status       = vehicle.Status,
            ProviderName = vehicle.Provider?.FullName ?? string.Empty
        };
    }

    // Full detail — assumes vehicle.Provider is loaded.
    public static VehicleDetailDto ToDetailDto(this Vehicle vehicle)
    {
        return new VehicleDetailDto
        {
            Id                 = vehicle.Id,
            ProviderId         = vehicle.ProviderId,
            ProviderName       = vehicle.Provider?.FullName ?? string.Empty,
            VehicleType        = vehicle.VehicleType,
            Model              = vehicle.Model,
            RegistrationNumber = vehicle.RegistrationNumber,
            Capacity           = vehicle.Capacity,
            PricePerDay        = vehicle.PricePerDay,
            Status             = vehicle.Status,
            CreatedAt          = vehicle.CreatedAt,
            UpdatedAt          = vehicle.UpdatedAt
        };
    }

    // Creates a new Vehicle entity from a CreateVehicleDto.
    // ProviderId and Status are set by the service, not here.
    public static Vehicle ToEntity(this CreateVehicleDto dto, int providerId)
    {
        return new Vehicle
        {
            ProviderId         = providerId,
            VehicleType        = dto.VehicleType,
            Model              = dto.Model,
            RegistrationNumber = dto.RegistrationNumber,
            Capacity           = dto.Capacity,
            PricePerDay        = dto.PricePerDay,
            Status             = VehicleStatus.PendingApproval,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };
    }

    // Updates an existing Vehicle entity with values from an UpdateVehicleDto.
    public static void UpdateFromDto(this Vehicle vehicle, UpdateVehicleDto dto)
    {
        vehicle.VehicleType        = dto.VehicleType;
        vehicle.Model              = dto.Model;
        vehicle.RegistrationNumber = dto.RegistrationNumber;
        vehicle.Capacity           = dto.Capacity;
        vehicle.PricePerDay        = dto.PricePerDay;
        vehicle.UpdatedAt          = DateTime.UtcNow;
    }
}

