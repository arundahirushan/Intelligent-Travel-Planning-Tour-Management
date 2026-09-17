using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TourManagement.Api.Configurations;
using TourManagement.Api.Data;
using TourManagement.Api.Extensions;
using TourManagement.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Read configuration ───────────────────────────────────────────────────────
// JwtSettings values come from user-secrets (dev) or environment variables (prod).
// Never hardcode secrets — run: dotnet user-secrets set "JwtSettings:SecretKey" "..."
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>() ?? throw new InvalidOperationException("JwtSettings not configured.");

// Register the settings object so services can inject it.
builder.Services.AddSingleton(jwtSettings);

// ── Controllers ─────────────────────────────────────────────────────────────
// Configure JSON to serialize enums as their string names (e.g. "Draft" not 0).
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── Swagger / OpenAPI ────────────────────────────────────────────────────────
// We add a "Bearer" auth option so you can paste a JWT directly in the Swagger UI
// and test protected endpoints without needing a separate tool like Postman.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TourManagement API", Version = "v1" });

    // Define the Bearer security scheme.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste your JWT here. Example: Bearer eyJhbGci..."
    });

    // Apply the scheme globally so every endpoint shows the lock icon.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ─────────────────────────────────────────────────────────────────────
// Allow the React dev server to call our API. The policy name is referenced below.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
        policy.WithOrigins("http://localhost:5173")  // React (Vite) dev server
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Database ─────────────────────────────────────────────────────────────────
// Connection string comes from user-secrets. Run:
//   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;..."
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentication & Authorization ──────────────────────────────────────────
builder.Services.AddJwtAuthentication(jwtSettings);
builder.Services.AddAuthorization();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddApplicationServices();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Seed the SuperAdmin user if one doesn't exist yet.
await SeedData.SeedAsync(app.Services);

// ── Middleware pipeline ──────────────────────────────────────────────────────
// ORDER IS IMPORTANT — exception handler first so it catches errors from everything below.

// 1. Global exception handler (must be first).
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Swagger (development only).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3. HTTPS redirect.
app.UseHttpsRedirection();

// 4. CORS — must come before authentication.
app.UseCors("AllowFrontends");

// 5. Authentication — reads the JWT from the Authorization header.
app.UseAuthentication();

// 6. Authorization — checks [Authorize] attributes after the user is identified.
app.UseAuthorization();

// 7. Map controller routes.
app.MapControllers();

app.Run();

// Makes the implicit Program class accessible for integration test projects.
public partial class Program { }
