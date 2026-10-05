using System.Net.Http.Headers;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Configurations;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

// Uploads listing photos (hotels, vehicles, destinations) to a public Supabase Storage bucket.
// We call the Storage REST API with HttpClient — the project guidelines do not allow
// Supabase client SDKs.
public class ImageUploadService : IImageUploadService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly HttpClient _http;
    private readonly SupabaseStorageSettings _settings;
    private readonly ILogger<ImageUploadService> _logger;

    public ImageUploadService(
        HttpClient http,
        SupabaseStorageSettings settings,
        ILogger<ImageUploadService> logger)
    {
        _http = http;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> UploadListingPhotoAsync(IFormFile? file, string? userRole)
    {
        // 1. Authorization: the folder comes from the JWT role, not from anything the client sends.
        var folder = GetFolderForRole(userRole);

        // 2. Basic file checks.
        if (file == null || file.Length == 0)
            throw new ValidationException("Please choose a photo to upload.");

        if (file.Length > MaxFileSizeBytes)
            throw new ValidationException("The photo is too large. The maximum size is 5 MB.");

        // 3. Read the bytes (at most 5 MB) and detect the real type from the file content.
        //    We do not trust the filename or the Content-Type sent by the client.
        byte[] bytes;
        using (var memory = new MemoryStream())
        {
            await file.CopyToAsync(memory);
            bytes = memory.ToArray();
        }

        var imageType = DetectImageType(bytes);
        if (imageType == null)
            throw new ValidationException("Only JPEG, PNG and WebP images are allowed.");

        // 4. Make sure the server is configured before we try to upload.
        if (string.IsNullOrWhiteSpace(_settings.Url)
            || string.IsNullOrWhiteSpace(_settings.ServiceRoleKey)
            || string.IsNullOrWhiteSpace(_settings.Bucket))
        {
            _logger.LogError("Supabase Storage is not configured (SupabaseStorage settings are missing).");
            throw new ValidationException("Photo upload is not configured on the server.");
        }

        // 5. Unique, generated object name — the original filename is never used.
        var objectPath = $"{folder}/{Guid.NewGuid():N}.{imageType.Value.Extension}";
        var baseUrl = _settings.Url.TrimEnd('/');

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/storage/v1/object/{_settings.Bucket}/{objectPath}");

        var cleanKey = _settings.ServiceRoleKey.Trim();
        request.Headers.Add("apikey", cleanKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanKey);

        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(imageType.Value.ContentType);

        // 6. Upload.
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Could not reach Supabase Storage.");
            throw new ValidationException($"Photo upload failed. Cannot reach Supabase: {ex.Message}");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Supabase Storage upload failed with status {Status}: {Body}",
                    (int)response.StatusCode, errorBody);
                throw new ValidationException($"Photo upload failed. Supabase returned {(int)response.StatusCode}: {errorBody}");
            }
        }

        // 7. The bucket is public, so the photo is readable at this predictable URL.
        return $"{baseUrl}/storage/v1/object/public/{_settings.Bucket}/{objectPath}";
    }

    // Each role may only upload photos for its own kind of listing.
    private static string GetFolderForRole(string? role)
    {
        return role switch
        {
            Roles.HotelOwner => "hotels",
            Roles.TransportProvider => "vehicles",
            Roles.Admin or Roles.SuperAdmin => "destinations",
            _ => throw new ForbiddenException("Your account is not allowed to upload listing photos.")
        };
    }

    // Looks at the first bytes of the file ("magic bytes") to identify JPEG, PNG or WebP.
    private static (string ContentType, string Extension)? DetectImageType(byte[] b)
    {
        // JPEG starts with FF D8 FF
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return ("image/jpeg", "jpg");

        // PNG starts with 89 50 4E 47 0D 0A 1A 0A
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return ("image/png", "png");

        // WebP is "RIFF" + 4 size bytes + "WEBP"
        if (b.Length >= 12
            && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
            return ("image/webp", "webp");

        return null;
    }
}

