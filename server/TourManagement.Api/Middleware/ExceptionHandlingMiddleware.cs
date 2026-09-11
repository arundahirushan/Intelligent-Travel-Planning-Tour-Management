namespace TourManagement.Api.Middleware;

// ExceptionHandlingMiddleware — placeholder
// Will catch unhandled exceptions globally and return a standard
// ApiResponse error payload with the appropriate HTTP status code.
// Registered in Program.cs via app.UseMiddleware<ExceptionHandlingMiddleware>().

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // InvokeAsync(HttpContext context) will be implemented later.
}
