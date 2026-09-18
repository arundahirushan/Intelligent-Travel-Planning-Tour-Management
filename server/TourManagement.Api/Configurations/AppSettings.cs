namespace TourManagement.Api.Configurations;

// Strongly-typed settings classes. Values are bound from appsettings.json
// and overridden by user-secrets / environment variables at runtime.
// Never commit real secrets — use "dotnet user-secrets set ..." instead.

// JWT signing / validation settings.
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    // How many hours until the token expires (default: 2).
    public int ExpiresInHours { get; set; } = 2;
}

// PayHere payment gateway configuration (used by a different team member's component).
public class PayHereSettings
{
    // Properties added later.
}

// Internal Python ai-service base URL and timeout settings.
public class AiServiceSettings
{
    public string BaseUrl { get; set; } = string.Empty;
}
