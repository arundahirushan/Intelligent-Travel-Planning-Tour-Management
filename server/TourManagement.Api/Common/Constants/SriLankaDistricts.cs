namespace TourManagement.Api.Common.Constants;

public static class SriLankaDistricts
{
    // 25 official districts of Sri Lanka and a representative central location for each.
    // Source: General geographical coordinates of major Sri Lankan district capitals.
    public static readonly IReadOnlyDictionary<string, (decimal Latitude, decimal Longitude)> Map = new Dictionary<string, (decimal Latitude, decimal Longitude)>(StringComparer.OrdinalIgnoreCase)
    {
        { "Ampara", (7.2912m, 81.6724m) },
        { "Anuradhapura", (8.3114m, 80.4037m) },
        { "Badulla", (6.9934m, 81.0550m) },
        { "Batticaloa", (7.7102m, 81.6924m) },
        { "Colombo", (6.9271m, 79.8612m) },
        { "Galle", (6.0535m, 80.2210m) },
        { "Gampaha", (7.0873m, 79.9928m) },
        { "Hambantota", (6.1246m, 81.1185m) },
        { "Jaffna", (9.6615m, 80.0255m) },
        { "Kalutara", (6.5854m, 79.9607m) },
        { "Kandy", (7.2906m, 80.6337m) },
        { "Kegalle", (7.2513m, 80.3464m) },
        { "Kilinochchi", (9.3803m, 80.3770m) },
        { "Kurunegala", (7.4818m, 80.3609m) },
        { "Mannar", (8.9810m, 79.9044m) },
        { "Matale", (7.4675m, 80.6234m) },
        { "Matara", (5.9549m, 80.5469m) },
        { "Moneragala", (6.8728m, 81.3507m) },
        { "Mullaitivu", (9.2671m, 80.8142m) },
        { "Nuwara Eliya", (6.9497m, 80.7891m) },
        { "Polonnaruwa", (7.9403m, 81.0188m) },
        { "Puttalam", (8.0298m, 79.8276m) },
        { "Ratnapura", (6.7056m, 80.3847m) },
        { "Trincomalee", (8.5811m, 81.2330m) },
        { "Vavuniya", (8.7542m, 80.4982m) }
    };
}
