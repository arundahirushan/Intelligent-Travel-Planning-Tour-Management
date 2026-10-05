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

        // Search by name or region.
        if (!string.IsNullOrEmpty(search))
            query = query.Where(d => d.Name.Contains(search) || d.Region.Contains(search));

        query = sort switch
        {
            "region"  => query.OrderBy(d => d.Region),
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
        if (!TourManagement.Api.Common.Constants.SriLankaDistricts.Map.TryGetValue(dto.Region, out var coords))
            throw new ValidationException($"Invalid district: {dto.Region}. Please select a valid Sri Lankan district.");

        var dest = dto.ToEntity();
        
        // Use canonical casing from the dictionary and set coordinates
        var canonicalDistrict = TourManagement.Api.Common.Constants.SriLankaDistricts.Map.Keys.First(k => k.Equals(dto.Region, StringComparison.OrdinalIgnoreCase));
        dest.Region = canonicalDistrict;
        dest.Latitude = coords.Latitude;
        dest.Longitude = coords.Longitude;

        _db.Destinations.Add(dest);
        await _db.SaveChangesAsync();
        return dest.ToResponseDto();
    }

    public async Task<DestinationResponseDto> UpdateAsync(int id, UpdateDestinationDto dto)
    {
        if (!TourManagement.Api.Common.Constants.SriLankaDistricts.Map.TryGetValue(dto.Region, out var coords))
            throw new ValidationException($"Invalid district: {dto.Region}. Please select a valid Sri Lankan district.");

        var dest = await _db.Destinations.FindAsync(id);
        if (dest == null)
            throw new NotFoundException($"Destination with ID {id} was not found.");

        dest.UpdateFromDto(dto);
        
        // Use canonical casing from the dictionary and set coordinates
        var canonicalDistrict = TourManagement.Api.Common.Constants.SriLankaDistricts.Map.Keys.First(k => k.Equals(dto.Region, StringComparison.OrdinalIgnoreCase));
        dest.Region = canonicalDistrict;
        dest.Latitude = coords.Latitude;
        dest.Longitude = coords.Longitude;

        await _db.SaveChangesAsync();
        return dest.ToResponseDto();
    }

    public async Task DeleteAsync(int id)
    {
        var dest = await _db.Destinations.FindAsync(id);
        if (dest == null)
            throw new NotFoundException($"Destination with ID {id} was not found.");

        // Guard 1: hotels reference this destination via DestinationId (Restrict delete behavior).
        // Check explicitly so the error message identifies the blocking entity clearly.
        bool hasHotels = await _db.Hotels.AnyAsync(h => h.DestinationId == id);
        if (hasHotels)
            throw new ValidationException(
                "Cannot delete this destination because one or more hotels are registered at it. " +
                "Reassign or remove those hotels first.");

        // Guard 2: itinerary items reference this destination in existing trip plans.
        bool hasItineraryItems = await _db.ItineraryItems.AnyAsync(i => i.DestinationId == id);
        if (hasItineraryItems)
            throw new ValidationException(
                "Cannot delete this destination because it is referenced by one or more trip itinerary items. " +
                "Remove those itinerary items first.");

        try
        {
            _db.Destinations.Remove(dest);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Database-level Restrict FK triggered — a concurrent insert added a reference
            // between the checks above and the SaveChanges call. Return a safe generic message.
            throw new ValidationException(
                "Cannot delete this destination because it is still referenced by other records. " +
                "Please refresh and try again.");
        }
    }
}
