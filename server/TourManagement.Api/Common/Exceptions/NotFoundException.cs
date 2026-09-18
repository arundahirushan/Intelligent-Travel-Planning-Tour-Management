namespace TourManagement.Api.Common.Exceptions;

// Thrown when a requested resource doesn't exist in the database.
// ExceptionHandlingMiddleware catches this and returns HTTP 404.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
