namespace TourManagement.Api.Common;

// ApiResponse — placeholder
// Standard envelope wrapping all API responses.
// Shape:  { success, message, data, errors }
// Real implementation (generics, error list, status codes) added later.

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}
