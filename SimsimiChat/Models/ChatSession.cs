using System;
using System.Collections.Generic;

namespace SimsimiChat.Models;

public partial class ChatSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? LastActiveAt { get; set; }
    
    public string DefaultRudenessLevel { get; set; } = "Neutral";

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User User { get; set; } = null!;
}
