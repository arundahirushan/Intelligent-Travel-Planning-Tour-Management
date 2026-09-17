namespace TourManagement.Api.Common;

// Standard wrapper for all API responses.
// Every endpoint returns ApiResponse<T> so the client always gets
// the same JSON shape: { success, message, data }
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    // Convenience factory — saves repeating the same object initializer
    // in every controller action.
    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }
}

// Non-generic version for responses that have no data payload (e.g. delete).
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null)
        => new ApiResponse { Success = true, Message = message };

    public static ApiResponse Fail(string message)
        => new ApiResponse { Success = false, Message = message };
}
