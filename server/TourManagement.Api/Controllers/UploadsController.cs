using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Uploads;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    private readonly IImageUploadService _imageUploadService;

    public UploadsController(IImageUploadService imageUploadService)
    {
        _imageUploadService = imageUploadService;
    }

    /// <summary>
    /// Upload one listing photo (multipart field "file"; JPEG, PNG or WebP, max 5 MB).
    /// HotelOwner uploads hotel photos, TransportProvider uploads vehicle photos,
    /// Admin / SuperAdmin upload destination photos. Returns the public image URL.
    /// </summary>
    [HttpPost("listing-photo")]
    [Authorize(Roles = $"{Roles.HotelOwner},{Roles.TransportProvider},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<ImageUploadResponseDto>>> UploadListingPhoto(IFormFile? file)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var imageUrl = await _imageUploadService.UploadListingPhotoAsync(file, role);

        var result = new ImageUploadResponseDto { ImageUrl = imageUrl };
        return Ok(ApiResponse<ImageUploadResponseDto>.Ok(result, "Photo uploaded."));
    }
}

