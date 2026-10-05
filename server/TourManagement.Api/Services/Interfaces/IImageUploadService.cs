namespace TourManagement.Api.Services.Interfaces;

public interface IImageUploadService
{
    // Validates the photo, uploads it to Supabase Storage and returns its public URL.
    // The storage folder is chosen from the caller's role (from the JWT), never from the client.
    Task<string> UploadListingPhotoAsync(IFormFile? file, string? userRole);
}

