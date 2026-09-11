namespace TourManagement.Api.Configurations;

// Placeholder options classes for strongly-typed configuration.
// Values bound from appsettings.json / environment variables / user-secrets.
// Never commit real secrets.

// JWT configuration (issuer, audience, secret key, expiry).
public class JwtSettings
{
    // Properties added later.
}

// PayHere payment gateway configuration (merchant ID, secret, etc.).
public class PayHereSettings
{
    // Properties added later.
}

// Internal Python ai-service base URL and timeout settings.
public class AiServiceSettings
{
    public string BaseUrl { get; set; } = string.Empty;
}
