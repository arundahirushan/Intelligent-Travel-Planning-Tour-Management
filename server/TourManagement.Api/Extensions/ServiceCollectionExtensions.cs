using Microsoft.IdentityModel.Tokens;
using System.Text;
using TourManagement.Api.Configurations;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Extensions;

// Extension methods that keep Program.cs short and readable.
// Each method groups a related block of DI registrations.
public static class ServiceCollectionExtensions
{
    // Registers JWT Bearer authentication with settings from JwtSettings.
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, JwtSettings jwtSettings)
    {
        services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings.Issuer,
                    ValidAudience            = jwtSettings.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                 Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };
            });

        return services;
    }

    // Registers all application services with scoped lifetime.
    // Scoped = one instance per HTTP request (shared within the same request).
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService,        TourManagement.Api.Services.Implementations.AuthService>();
        services.AddScoped<IUserService,        TourManagement.Api.Services.Implementations.UserService>();
        services.AddScoped<IDestinationService, TourManagement.Api.Services.Implementations.DestinationService>();
        services.AddScoped<ITripService,        TourManagement.Api.Services.Implementations.TripService>();
        services.AddScoped<IHotelService,       TourManagement.Api.Services.Implementations.HotelService>();
        services.AddScoped<IBookingService,     TourManagement.Api.Services.Implementations.BookingService>();

        return services;
    }
}
