using System.Net;
using System.Text.Json;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Exceptions;

// Alias so our custom ValidationException doesn't clash with the System one.
using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Middleware;

// Sits at the very start of the pipeline and catches any unhandled exception
// from any controller or service. Returns a consistent JSON error shape
// without leaking stack traces to the client.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            // Resource doesn't exist — tell the client clearly.
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            // User is authenticated but not allowed to touch this resource.
            await WriteErrorResponse(context, HttpStatusCode.Forbidden, ex.Message);
        }
        catch (ValidationException ex)
        {
            // Business rule violation — safe to show the message to the client.
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            // Something unexpected — log it but don't expose details to the client.
            _logger.LogError(ex, "An unexpected error occurred.");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
