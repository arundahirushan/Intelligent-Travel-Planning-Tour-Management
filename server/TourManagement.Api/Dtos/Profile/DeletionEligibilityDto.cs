namespace TourManagement.Api.Dtos.Profile;

public class DeletionEligibilityDto
{
    public bool CanDelete { get; set; }
    public int BlockingCount { get; set; }
    public string? BlockingMessage { get; set; }
}
