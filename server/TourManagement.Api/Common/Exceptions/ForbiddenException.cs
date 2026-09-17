namespace TourManagement.Api.Common.Exceptions;

// Thrown when a user tries to access a resource they don't own.
// For example, a traveler trying to edit someone else's trip.
// ExceptionHandlingMiddleware catches this and returns HTTP 403.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You are not allowed to perform this action.")
        : base(message) { }
}
