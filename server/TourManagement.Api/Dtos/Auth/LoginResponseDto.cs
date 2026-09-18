namespace TourManagement.Api.Dtos.Auth;

// What the API returns after a successful login.
public class LoginResponseDto
{
    // The JWT the client should include in future requests
    // as: Authorization: Bearer <token>
    public string Token { get; set; } = string.Empty;

    // When the token expires — the client can use this to know
    // when to prompt the user to log in again.
    public DateTime ExpiresAt { get; set; }

    // Basic user info so the client can display the logged-in user's name/role
    // without needing a separate API call.
    public UserSummaryInToken User { get; set; } = new();

    public class UserSummaryInToken
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
