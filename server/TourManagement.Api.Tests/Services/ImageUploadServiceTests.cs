using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Configurations;
using TourManagement.Api.Services.Implementations;

using ForbiddenException = TourManagement.Api.Common.Exceptions.ForbiddenException;
using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Tests.Services;

// Tests for the listing photo upload rules. No real Supabase calls are made:
// a fake HTTP handler stands in for the Storage API.
public class ImageUploadServiceTests
{
    // Records the request and returns a fixed status code.
    private class FakeHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public HttpRequestMessage? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent("{}") });
        }
    }

    private static (ImageUploadService Service, FakeHandler Handler) CreateService(bool configured = true)
    {
        var handler = new FakeHandler();
        var settings = new SupabaseStorageSettings
        {
            Url = configured ? "https://example.supabase.co/" : "",
            ServiceRoleKey = configured ? "test-key" : "",
            Bucket = "listing-photos"
        };
        var service = new ImageUploadService(new HttpClient(handler), settings, NullLogger<ImageUploadService>.Instance);
        return (service, handler);
    }

    private static IFormFile MakeFile(byte[] bytes, string fileName = "photo.jpg")
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    private static readonly byte[] JpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
    private static readonly byte[] WebpBytes =
        { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' };

    [Fact]
    public async Task Upload_Jpeg_ReturnsPublicUrl_AndSendsKeyToStorage()
    {
        var (service, handler) = CreateService();

        var url = await service.UploadListingPhotoAsync(MakeFile(JpegBytes), Roles.HotelOwner);

        Assert.StartsWith("https://example.supabase.co/storage/v1/object/public/listing-photos/hotels/", url);
        Assert.EndsWith(".jpg", url);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.StartsWith("https://example.supabase.co/storage/v1/object/listing-photos/hotels/",
            handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("test-key", handler.LastRequest.Headers.GetValues("apikey").Single());
    }

    [Fact]
    public async Task Upload_PngAndWebp_AreAccepted()
    {
        var (service, _) = CreateService();

        var png = await service.UploadListingPhotoAsync(MakeFile(PngBytes, "a.png"), Roles.HotelOwner);
        var webp = await service.UploadListingPhotoAsync(MakeFile(WebpBytes, "a.webp"), Roles.HotelOwner);

        Assert.EndsWith(".png", png);
        Assert.EndsWith(".webp", webp);
    }

    [Theory]
    [InlineData(Roles.HotelOwner, "/hotels/")]
    [InlineData(Roles.TransportProvider, "/vehicles/")]
    [InlineData(Roles.Admin, "/destinations/")]
    [InlineData(Roles.SuperAdmin, "/destinations/")]
    public async Task Upload_FolderIsChosenFromRole(string role, string expectedFolder)
    {
        var (service, _) = CreateService();

        var url = await service.UploadListingPhotoAsync(MakeFile(JpegBytes), role);

        Assert.Contains(expectedFolder, url);
    }

    [Theory]
    [InlineData(Roles.Traveler)]
    [InlineData(Roles.Supplier)]
    [InlineData(null)]
    public async Task Upload_Blocked_ForOtherRoles(string? role)
    {
        var (service, handler) = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.UploadListingPhotoAsync(MakeFile(JpegBytes), role));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Upload_Rejects_NonImageEvenWithJpgFilename()
    {
        var (service, handler) = CreateService();
        var textBytes = System.Text.Encoding.UTF8.GetBytes("this is not an image");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadListingPhotoAsync(MakeFile(textBytes, "evil.jpg"), Roles.HotelOwner));

        Assert.Contains("JPEG, PNG and WebP", ex.Message);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Upload_Rejects_FileLargerThan5MB()
    {
        var (service, handler) = CreateService();
        var big = new byte[5 * 1024 * 1024 + 1];
        JpegBytes.CopyTo(big, 0);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadListingPhotoAsync(MakeFile(big), Roles.HotelOwner));

        Assert.Contains("5 MB", ex.Message);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Upload_Rejects_MissingOrEmptyFile()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadListingPhotoAsync(null, Roles.HotelOwner));
        await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadListingPhotoAsync(MakeFile(Array.Empty<byte>()), Roles.HotelOwner));
    }

    [Fact]
    public async Task Upload_IgnoresClientFilename_AndGeneratesUniqueNames()
    {
        var (service, _) = CreateService();

        var url1 = await service.UploadListingPhotoAsync(MakeFile(JpegBytes, "../../secret name.jpg"), Roles.HotelOwner);
        var url2 = await service.UploadListingPhotoAsync(MakeFile(JpegBytes, "../../secret name.jpg"), Roles.HotelOwner);

        Assert.DoesNotContain("secret", url1);
        Assert.DoesNotContain("..", url1);
        Assert.NotEqual(url1, url2);
    }

    [Fact]
    public async Task Upload_StorageFailure_ThrowsFriendlyError()
    {
        var (service, handler) = CreateService();
        handler.StatusCode = HttpStatusCode.InternalServerError;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadListingPhotoAsync(MakeFile(JpegBytes), Roles.HotelOwner));

        Assert.Contains("Photo upload failed", ex.Message);
    }

    [Fact]
    public async Task Upload_NotConfigured_ThrowsClearError()
    {
        var (service, handler) = CreateService(configured: false);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadListingPhotoAsync(MakeFile(JpegBytes), Roles.HotelOwner));

        Assert.Contains("not configured", ex.Message);
        Assert.Equal(0, handler.CallCount);
    }
}

