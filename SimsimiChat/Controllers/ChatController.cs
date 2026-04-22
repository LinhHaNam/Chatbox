using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimsimiChat.Models;
using SimsimiChat.Models.Dtos;
using SimsimiChat.Services;

namespace SimsimiChat.Controllers;

/// <summary>
/// Chat controller for handling chat sessions and messages with AI
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IAiService _aiService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IAiService aiService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest? request = null)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var session = await _chatService.CreateSessionAsync(
                userId,
                (request?.DefaultRudenessLevel ?? RudenessLevel.Neutral).ToString());

            var sessionDto = MapToChatSessionDto(session);
            return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session");
            return StatusCode(500, new { success = false, message = "Error creating chat session" });
        }
    }

    /// <summary>
    /// Update a chat session's default rudeness level
    /// </summary>
    [HttpPatch("sessions/{sessionId}/rudeness-level")]
    public async Task<IActionResult> UpdateSessionRudenessLevel(Guid sessionId, [FromBody] UpdateSessionRudenessLevelRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var session = await _chatService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            if (session.UserId != userId)
            {
                _logger.LogWarning("Unauthorized rudeness-level update attempt to session {SessionId} by user {UserId}", sessionId, userId);
                return Forbid("You don't have access to this chat session");
            }

            var updatedSession = await _chatService.UpdateSessionRudenessLevelAsync(sessionId, request.RudenessLevel.ToString());
            if (updatedSession == null)
            {
                return StatusCode(500, new { success = false, message = "Error updating session rudeness level" });
            }

            return Ok(MapToChatSessionDto(updatedSession));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session rudeness level");
            return StatusCode(500, new { success = false, message = "Error updating session rudeness level" });
        }
    }

    /// <summary>
    /// Get chat session with messages
    /// </summary>
    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var session = await _chatService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            // Verify user owns this session
            if (session.UserId != userId)
            {
                _logger.LogWarning($"Unauthorized access attempt to session {sessionId} by user {userId}");
                return Forbid("You don't have access to this chat session");
            }

            var sessionDto = MapToChatSessionDto(session);
            return Ok(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat session");
            return StatusCode(500, new { success = false, message = "Error retrieving chat session" });
        }
    }

    /// <summary>
    /// Get all chat sessions for the current user
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetUserSessions()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var sessions = await _chatService.GetUserSessionsAsync(userId);
            var sessionDtos = sessions.Select(MapToChatSessionDto).ToList();

            return Ok(new { success = true, data = sessionDtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user sessions");
            return StatusCode(500, new { success = false, message = "Error retrieving sessions" });
        }
    }

    /// <summary>
    /// Send a message and get AI response
    /// </summary>
    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AiResponseDto
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new AiResponseDto
                {
                    Success = false,
                    Message = "Invalid user token"
                });
            }

            // Verify session belongs to user
            var session = await _chatService.GetSessionAsync(request.SessionId);
            if (session == null)
            {
                return NotFound(new AiResponseDto
                {
                    Success = false,
                    Message = "Chat session not found"
                });
            }

            if (session.UserId != userId)
            {
                _logger.LogWarning($"Unauthorized message attempt to session {request.SessionId} by user {userId}");
                return Forbid("You don't have access to this chat session");
            }

            // Save user message
            var userMessage = await _chatService.SaveMessageAsync(
                request.SessionId, 
                "User", 
                request.Content);

            var sessionMessages = await _chatService.GetSessionMessagesAsync(request.SessionId);

            // Get AI response
            var aiResponse = await _aiService.GetResponseAsync(request.Content, request.RudenessLevel, sessionMessages);

            // Save AI response
            var botMessage = await _chatService.SaveMessageAsync(
                request.SessionId, 
                "Bot", 
                aiResponse);

            _logger.LogInformation($"Message processed for session {request.SessionId} with rudeness level: {request.RudenessLevel}");

            return Ok(new AiResponseDto
            {
                Success = true,
                Message = "Message processed successfully",
                UserMessage = MapToMessageDto(userMessage),
                BotMessage = MapToMessageDto(botMessage)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return StatusCode(500, new AiResponseDto
            {
                Success = false,
                Message = "Error processing message"
            });
        }
    }

    /// <summary>
    /// Delete a chat session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var session = await _chatService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            if (session.UserId != userId)
            {
                _logger.LogWarning($"Unauthorized delete attempt to session {sessionId} by user {userId}");
                return Forbid("You don't have access to this chat session");
            }

            var success = await _chatService.DeleteSessionAsync(sessionId);
            if (!success)
            {
                return StatusCode(500, new { success = false, message = "Error deleting session" });
            }

            return Ok(new { success = true, message = "Chat session deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chat session");
            return StatusCode(500, new { success = false, message = "Error deleting session" });
        }
    }

    /// <summary>
    /// Get user ID from JWT token claims
    /// </summary>
    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Guid.Empty;
        }
        return userId;
    }

    /// <summary>
    /// Map Message entity to MessageDto
    /// </summary>
    private MessageDto MapToMessageDto(Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            SessionId = message.SessionId,
            SenderType = message.SenderType,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }

    /// <summary>
    /// Map ChatSession entity to ChatSessionDto
    /// </summary>
    private ChatSessionDto MapToChatSessionDto(ChatSession session)
    {
        return new ChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            StartedAt = session.StartedAt,
            LastActiveAt = session.LastActiveAt,
            DefaultRudenessLevel = ParseRudenessLevel(session.DefaultRudenessLevel),
            Messages = session.Messages?.Select(MapToMessageDto).ToList()
        };
    }

    private RudenessLevel ParseRudenessLevel(string? value)
    {
        return Enum.TryParse<RudenessLevel>(value, true, out var parsed)
            ? parsed
            : RudenessLevel.Neutral;
    }
}
