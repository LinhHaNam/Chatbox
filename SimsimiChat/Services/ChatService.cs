using Microsoft.EntityFrameworkCore;
using SimsimiChat.Models;
using SimsimiChat.Models.Dtos;

namespace SimsimiChat.Services;

/// <summary>
/// Chat service for managing chat sessions and messages
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Create a new chat session for a user
    /// </summary>
    Task<ChatSession> CreateSessionAsync(Guid userId, string defaultRudenessLevel = "Neutral");
    
    /// <summary>
    /// Get chat session by ID
    /// </summary>
    Task<ChatSession?> GetSessionAsync(Guid sessionId);
    
    /// <summary>
    /// Get all chat sessions for a user
    /// </summary>
    Task<List<ChatSession>> GetUserSessionsAsync(Guid userId);
    
    /// <summary>
    /// Save a message to the database
    /// </summary>
    Task<Message> SaveMessageAsync(Guid sessionId, string senderType, string content);
    
    /// <summary>
    /// Get chat history for a session
    /// </summary>
    Task<List<Message>> GetSessionMessagesAsync(Guid sessionId);

    /// <summary>
    /// Update the default rudeness level for a session
    /// </summary>
    Task<ChatSession?> UpdateSessionRudenessLevelAsync(Guid sessionId, string defaultRudenessLevel);
    
    /// <summary>
    /// Delete a chat session
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid sessionId);
}

public class ChatService : IChatService
{
    private readonly SimsimiDbContext _context;
    private readonly ILogger<ChatService> _logger;

    public ChatService(SimsimiDbContext context, ILogger<ChatService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    public async Task<ChatSession> CreateSessionAsync(Guid userId, string defaultRudenessLevel = "Neutral")
    {
        try
        {
            var session = new ChatSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow,
                DefaultRudenessLevel = defaultRudenessLevel
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Chat session created: {session.Id} for user: {userId}");
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session");
            throw;
        }
    }

    /// <summary>
    /// Get chat session by ID with messages
    /// </summary>
    public async Task<ChatSession?> GetSessionAsync(Guid sessionId)
    {
        try
        {
            return await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat session");
            return null;
        }
    }

    /// <summary>
    /// Get all chat sessions for a user
    /// </summary>
    public async Task<List<ChatSession>> GetUserSessionsAsync(Guid userId)
    {
        try
        {
            return await _context.ChatSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.LastActiveAt)
                .Include(s => s.Messages)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user sessions");
            return new List<ChatSession>();
        }
    }

    /// <summary>
    /// Save a message to the database
    /// </summary>
    public async Task<Message> SaveMessageAsync(Guid sessionId, string senderType, string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content cannot be empty", nameof(content));
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                SenderType = senderType, // "User" or "Bot"
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            
            // Update session's last active time
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastActiveAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Message saved: {message.Id} in session: {sessionId}");
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message");
            throw;
        }
    }

    /// <summary>
    /// Get all messages for a session
    /// </summary>
    public async Task<List<Message>> GetSessionMessagesAsync(Guid sessionId)
    {
        try
        {
            return await _context.Messages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages");
            return new List<Message>();
        }
    }

    /// <summary>
    /// Update the default rudeness level for a session
    /// </summary>
    public async Task<ChatSession?> UpdateSessionRudenessLevelAsync(Guid sessionId, string defaultRudenessLevel)
    {
        try
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return null;
            }

            session.DefaultRudenessLevel = defaultRudenessLevel;
            session.LastActiveAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} updated with rudeness level {RudenessLevel}", sessionId, defaultRudenessLevel);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session rudeness level");
            return null;
        }
    }

    /// <summary>
    /// Delete a chat session and its messages
    /// </summary>
    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session == null)
            {
                return false;
            }

            // Delete all messages in the session
            var messages = await _context.Messages
                .Where(m => m.SessionId == sessionId)
                .ToListAsync();

            _context.Messages.RemoveRange(messages);
            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Chat session deleted: {sessionId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chat session");
            return false;
        }
    }
}
