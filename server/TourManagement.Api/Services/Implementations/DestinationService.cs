using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Destination;
using TourManagement.Api.Mappings;
using TourManagement.Api.Services.Interfaces;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Services.Implementations;

public class DestinationService : IDestinationService
{
    private readonly AppDbContext _db;

    public DestinationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<DestinationResponseDto>> GetAllAsync(
        string? search, string? sort, int page, int pageSize)
    {
        var query = _db.Destinations.AsQueryable();

        // Search by name or country.
        if (!string.IsNullOrEmpty(search))
            query = query.Where(d => d.Name.Contains(search) || d.Country.Contains(search));

        query = sort switch
        {
            "country" => query.OrderBy(d => d.Country),
            "oldest"  => query.OrderBy(d => d.CreatedAt),
            _         => query.OrderBy(d => d.Name)  // default: alphabetical by name
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => d.ToResponseDto())
            .ToListAsync();

        return new PagedResult<DestinationResponseDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<DestinationResponseDto> GetByIdAsync(int id)
    {
        var dest = await _db.Destinations.FindAsync(id);
        if (dest == null)
            throw new NotFoundException($"Destination with ID {id} was not found.");

        return dest.ToResponseDto();
    }

    public async Task<DestinationResponseDto> CreateAsync(CreateDestinationDto dto)
    {
        var dest = dto.ToEntity();
        _db.Destinations.Add(dest);
        await _db.SaveChangesAsync();
        return dest.ToResponseDto();
    }

    public async Task<DestinationResponseDto> UpdateAsync(int id, UpdateDestinationDto dto)
    {
        var dest = await _db.Destinations.FindAsync(id);
        if (dest == null)
            throw new NotFoundException($"Destination with ID {id} was not found.");

        dest.UpdateFromDto(dto);
        await _db.SaveChangesAsync();
        return dest.ToResponseDto();
    }

    public async Task DeleteAsync(int id)
    {
        var dest = await _db.Destinations.FindAsync(id);
        if (dest == null)
            throw new NotFoundException($"Destination with ID {id} was not found.");

        try
        {
            _db.Destinations.Remove(dest);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The database blocked the delete because this destination is still
            // referenced by at least one itinerary item (Restrict delete behavior).
            throw new ValidationException(
                "Cannot delete this destination because it is referenced by one or more itinerary items. " +
                "Remove those itinerary items first.");
        }
    }
}
