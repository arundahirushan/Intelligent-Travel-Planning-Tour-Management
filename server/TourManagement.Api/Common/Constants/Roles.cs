namespace TourManagement.Api.Common.Constants;

// Role names as string constants.
// We use strings (not an enum) because ASP.NET Core's [Authorize(Roles = ...)]
// attribute requires a string at compile time, so using constants avoids
// having to call .ToString() everywhere.
public static class Roles
{
    public const string Traveler          = "Traveler";
    public const string HotelOwner        = "HotelOwner";
    public const string TransportProvider = "TransportProvider";
    public const string Supplier          = "Supplier";
    public const string Admin             = "Admin";
    public const string SuperAdmin        = "SuperAdmin";
}
