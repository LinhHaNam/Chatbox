using System;
using System.Collections.Generic;

namespace SimsimiChat.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string? DeviceId { get; set; }

    public string? Username { get; set; }

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = null!;

    public bool IsBanned { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
