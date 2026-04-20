using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimsimiChat.Models;
using SimsimiChat.Models.Dtos;
using SimsimiChat.Services;

namespace SimsimiChat.Controllers;

/// <summary>
/// Authentication controller for user registration, login, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authService;
    private readonly SimsimiDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        IAuthenticationService authService,
        SimsimiDbContext context,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _authService = authService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        var (success, message, user) = await _userService.RegisterAsync(request.Username, request.Password);

        if (!success)
        {
            _logger.LogWarning($"Registration failed for username: {request.Username}");
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = message
            });
        }

        var userDto = MapToUserDto(user!);
        return Ok(new AuthResponse
        {
            Success = true,
            Message = message,
            User = userDto
        });
    }

    /// <summary>
    /// Login user with username and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        var user = await _userService.FindByUsernameAsync(request.Username);

        if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash!))
        {
            _logger.LogWarning($"Login failed for username: {request.Username}");
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid username or password"
            });
        }

        if (user.IsBanned)
        {
            _logger.LogWarning($"Login attempted by banned user: {user.Id}");
            return Forbid("User account is banned");
        }

        // Generate tokens
        var accessToken = _authService.GenerateAccessToken(user);
        var refreshToken = _authService.GenerateRefreshToken();

        // Save refresh token to database
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            JwtId = user.Id.ToString(),
            UserId = user.Id,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        
        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        _context.Users.Update(user);
        
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User logged in successfully: {user.Id}");

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = MapToUserDto(user)
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Refresh token is required"
            });
        }

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || 
            storedToken.ExpiryDate < DateTime.UtcNow)
        {
            _logger.LogWarning($"Invalid refresh token attempt");
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            });
        }

        var user = await _userService.FindByIdAsync(storedToken.UserId);
        if (user == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        // Generate new tokens
        var newAccessToken = _authService.GenerateAccessToken(user);
        var newRefreshToken = _authService.GenerateRefreshToken();

        // Mark old refresh token as used and create new one
        storedToken.IsUsed = true;
        _context.RefreshTokens.Update(storedToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            JwtId = user.Id.ToString(),
            UserId = user.Id,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Token refreshed for user: {user.Id}");

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            User = MapToUserDto(user)
        });
    }

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (storedToken != null)
            {
                storedToken.IsRevoked = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation($"User logged out: {userId}");

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { success = false, message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Map User entity to UserDto
    /// </summary>
    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}
