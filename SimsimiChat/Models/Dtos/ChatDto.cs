namespace SimsimiChat.Models.Dtos;

/// <summary>
/// Rudeness level enumeration for AI responses
/// </summary>
public enum RudenessLevel
{
    Polite,        // Respectful and helpful
    Neutral,       // Direct and straightforward
    Casual,        // Friendly and relaxed
    Sarcastic,     // Sarcastic but not offensive
    Rude           // Blunt and aggressive
}

/// <summary>
/// Request model for sending a chat message
/// </summary>
public class SendMessageRequest
{
    public Guid SessionId { get; set; }
    public string Content { get; set; } = null!;
    public RudenessLevel RudenessLevel { get; set; } = RudenessLevel.Neutral;
}

/// <summary>
/// Response model for chat message
/// </summary>
public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int? SequenceNumber { get; set; }
    public string SenderType { get; set; } = null!;  // "User" or "Bot"
    public string Content { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Response model for AI-generated response
/// </summary>
public class AiResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public MessageDto? UserMessage { get; set; }
    public MessageDto? BotMessage { get; set; }
}

/// <summary>
/// Request model to create a new chat session
/// </summary>
public class CreateChatSessionRequest
{
    public string? SessionTitle { get; set; }
    public RudenessLevel DefaultRudenessLevel { get; set; } = RudenessLevel.Neutral;
}

/// <summary>
/// Request model to update a session's default rudeness level
/// </summary>
public class UpdateSessionRudenessLevelRequest
{
    public RudenessLevel RudenessLevel { get; set; } = RudenessLevel.Neutral;
}

/// <summary>
/// Response model for chat session
/// </summary>
public class ChatSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public RudenessLevel DefaultRudenessLevel { get; set; } = RudenessLevel.Neutral;
    public List<MessageDto>? Messages { get; set; }
}
