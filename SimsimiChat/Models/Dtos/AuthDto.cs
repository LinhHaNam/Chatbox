namespace SimsimiChat.Models.Dtos;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

/// <summary>
/// Request model for token refresh
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
}

/// <summary>
/// Response model for authentication
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }
}

/// <summary>
/// User information DTO
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string Role { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
}
