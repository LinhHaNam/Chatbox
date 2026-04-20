using Microsoft.EntityFrameworkCore;
using SimsimiChat.Models;
using SimsimiChat.Models.Dtos;

namespace SimsimiChat.Services;

/// <summary>
/// User service for managing user operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string password);
    
    /// <summary>
    /// Find user by username
    /// </summary>
    Task<User?> FindByUsernameAsync(string username);
    
    /// <summary>
    /// Find user by ID
    /// </summary>
    Task<User?> FindByIdAsync(Guid userId);
    
    /// <summary>
    /// Check if username already exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);
}

public class UserService : IUserService
{
    private readonly SimsimiDbContext _context;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<UserService> _logger;

    public UserService(SimsimiDbContext context, IAuthenticationService authService, ILogger<UserService> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with validation
    /// </summary>
    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string password)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        {
            return (false, "Username must be at least 3 characters long", null);
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return (false, "Password must be at least 6 characters long", null);
        }

        // Check if user already exists
        if (await UsernameExistsAsync(username))
        {
            return (false, "Username already exists", null);
        }

        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username.ToLower(),
                PasswordHash = _authService.HashPassword(password),
                Role = "User",
                IsBanned = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User registered successfully: {username}");
            return (true, "User registered successfully", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return (false, "An error occurred during registration", null);
        }
    }

    /// <summary>
    /// Find user by username (case-insensitive)
    /// </summary>
    public async Task<User?> FindByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => 
                u.Username!.ToLower() == username.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by username");
            return null;
        }
    }

    /// <summary>
    /// Find user by ID
    /// </summary>
    public async Task<User?> FindByIdAsync(Guid userId)
    {
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by ID");
            return null;
        }
    }

    /// <summary>
    /// Check if username exists
    /// </summary>
    public async Task<bool> UsernameExistsAsync(string username)
    {
        try
        {
            return await _context.Users.AnyAsync(u => 
                u.Username!.ToLower() == username.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if username exists");
            return false;
        }
    }
}
