namespace TourManagement.Api.Common.Exceptions;

// Thrown for business rule violations that the client should be told about.
// For example: EndDate before StartDate, email already in use, etc.
// ExceptionHandlingMiddleware catches this and returns HTTP 400.
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
