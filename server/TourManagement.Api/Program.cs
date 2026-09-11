// TourManagement.Api — Program.cs
// Minimal startup: Swagger, Controllers, and a CORS placeholder.
// Authentication, EF Core, and middleware will be wired up in later prompts.

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// TODO (later prompt): Add CORS policy
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowFrontends", policy =>
//         policy.WithOrigins("http://localhost:5173")   // React dev
//               .AllowAnyHeader()
//               .AllowAnyMethod());
// });

// TODO (later prompt): Register EF Core DbContext
// builder.Services.AddDbContext<AppDbContext>(...);

// TODO (later prompt): Register JWT authentication
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)...

// TODO (later prompt): Register AgentServiceClient (HttpClient factory)
// builder.Services.AddHttpClient<IAgentServiceClient, AgentServiceClient>(...);

// ── Pipeline ─────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// TODO (later prompt): app.UseCors("AllowFrontends");
// TODO (later prompt): app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
