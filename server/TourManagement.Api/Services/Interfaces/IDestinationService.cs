using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Destination;

namespace TourManagement.Api.Services.Interfaces;

public interface IDestinationService
{
    // Any authenticated user can browse destinations.
    Task<PagedResult<DestinationResponseDto>> GetAllAsync(
        string? search, string? sort, int page, int pageSize);

    Task<DestinationResponseDto> GetByIdAsync(int id);

    // Admin / SuperAdmin only.
    Task<DestinationResponseDto> CreateAsync(CreateDestinationDto dto);
    Task<DestinationResponseDto> UpdateAsync(int id, UpdateDestinationDto dto);
    Task DeleteAsync(int id);
}
