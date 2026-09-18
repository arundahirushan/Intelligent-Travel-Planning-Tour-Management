using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Destination;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/destinations")]
public class DestinationsController : ControllerBase
{
    private readonly IDestinationService _destinationService;

    public DestinationsController(IDestinationService destinationService)
    {
        _destinationService = destinationService;
    }

    /// <summary>List destinations with optional search, sort, and pagination. Any authenticated user.</summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResult<DestinationResponseDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _destinationService.GetAllAsync(search, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<DestinationResponseDto>>.Ok(result));
    }

    /// <summary>Get a single destination by ID. Any authenticated user.</summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DestinationResponseDto>>> GetById(int id)
    {
        var result = await _destinationService.GetByIdAsync(id);
        return Ok(ApiResponse<DestinationResponseDto>.Ok(result));
    }

    /// <summary>Create a new destination. Admin / SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<DestinationResponseDto>>> Create([FromBody] CreateDestinationDto dto)
    {
        var result = await _destinationService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<DestinationResponseDto>.Ok(result, "Destination created."));
    }

    /// <summary>Update a destination. Admin / SuperAdmin only.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<DestinationResponseDto>>> Update(int id, [FromBody] UpdateDestinationDto dto)
    {
        var result = await _destinationService.UpdateAsync(id, dto);
        return Ok(ApiResponse<DestinationResponseDto>.Ok(result, "Destination updated."));
    }

    /// <summary>Delete a destination. Admin / SuperAdmin only.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _destinationService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Destination deleted."));
    }
}
