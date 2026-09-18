using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Configurations;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Auth;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

// Alias to avoid ambiguity with System.ComponentModel.DataAnnotations.ValidationException.
using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext db, JwtSettings jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        // Admin and SuperAdmin accounts must be created by a SuperAdmin through
        // a different endpoint — never through public registration.
        if (dto.Role == Roles.Admin || dto.Role == Roles.SuperAdmin)
        {
            throw new ValidationException("Admin and SuperAdmin accounts cannot be created through public registration.");
        }

        // Reject unknown roles so we don't store garbage in the database.
        var validRoles = new[] { Roles.Traveler, Roles.HotelOwner, Roles.TransportProvider, Roles.Supplier };
        if (!validRoles.Contains(dto.Role))
        {
            throw new ValidationException($"'{dto.Role}' is not a valid role for registration.");
        }

        // Email must be unique across all users.
        bool emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
        {
            throw new ValidationException("This email address is already registered.");
        }

        // Determine account status based on role.
        // Travelers can log in immediately; providers wait for admin approval.
        var status = dto.Role == Roles.Traveler
            ? UserStatus.Active
            : UserStatus.PendingApproval;

        var user = new User
        {
            FullName     = dto.FullName,
            Email        = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),  // never store raw password
            Role         = dto.Role,
            Status       = status,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Give the client a clear message about what happens next.
        var message = status == UserStatus.Active
            ? "Registration successful. You can now log in."
            : "Registration successful. Your account is awaiting admin approval before you can log in.";

        return new RegisterResponseDto
        {
            Id      = user.Id,
            FullName = user.FullName,
            Email   = user.Email,
            Role    = user.Role,
            Status  = user.Status,
            Message = message
        };
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        // Find user by email. We use the same generic error message whether
        // the email doesn't exist or the password is wrong — this prevents
        // attackers from discovering which emails are registered.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        bool passwordValid = user != null && BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            throw new ValidationException("Invalid email or password.");
        }

        // At this point we know the credentials are correct.
        // Now check whether the account is allowed to log in.
        switch (user!.Status)
        {
            case UserStatus.PendingApproval:
                throw new ValidationException("Your account is awaiting admin approval.");
            case UserStatus.Rejected:
                throw new ValidationException("Your registration was rejected.");
            case UserStatus.Suspended:
                throw new ValidationException("Your account has been suspended.");
        }

        // Generate and return a JWT.
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpiresInHours);
        var token = GenerateJwt(user, expiresAt);

        return new LoginResponseDto
        {
            Token     = token,
            ExpiresAt = expiresAt,
            User = new LoginResponseDto.UserSummaryInToken
            {
                Id       = user.Id,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = user.Role
            }
        };
    }

    // Builds a signed JWT containing the user's Id, Email, and Role as claims.
    private string GenerateJwt(User user, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // ClaimTypes.NameIdentifier is the standard claim for user ID.
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            // ClaimTypes.Role is what ASP.NET Core reads for [Authorize(Roles = ...)].
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer:   _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims:   claims,
            expires:  expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
