using System;
using System.Collections.Generic;

namespace SimsimiChat.Models;

public partial class Message
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public int? SequenceNumber { get; set; }

    public string SenderType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ChatSession Session { get; set; } = null!;
}
