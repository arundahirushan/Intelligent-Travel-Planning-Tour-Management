namespace TourManagement.Api.Configurations;

// Settings for uploading listing photos to Supabase Storage.
// Url and ServiceRoleKey are secrets/environment-specific: set them with
// user-secrets (dev) or environment variables (prod), never in appsettings.json.
public class SupabaseStorageSettings
{
    // Project URL, e.g. https://abcdxyz.supabase.co
    public string Url { get; set; } = string.Empty;

    // Server-side key. Stays on the backend only — never sent to React or Flutter.
    public string ServiceRoleKey { get; set; } = string.Empty;

    // Name of the PUBLIC bucket that holds hotel, vehicle and destination photos.
    public string Bucket { get; set; } = "listing-photos";
}

