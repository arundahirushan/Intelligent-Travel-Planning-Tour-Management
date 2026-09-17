using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Auth;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

// Public endpoints — no [Authorize] needed here.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new account (Traveler, HotelOwner, TransportProvider, or Supplier).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> Register([FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(ApiResponse<RegisterResponseDto>.Ok(result, result.Message));
    }

    /// <summary>Log in and receive a JWT token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }
}
