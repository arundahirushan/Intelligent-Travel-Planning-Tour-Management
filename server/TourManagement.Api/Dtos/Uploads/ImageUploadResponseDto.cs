namespace TourManagement.Api.Dtos.Uploads;

public class ImageUploadResponseDto
{
    // Public URL of the uploaded photo. The client saves this in the entity's ImageUrl field.
    public string ImageUrl { get; set; } = string.Empty;
}

