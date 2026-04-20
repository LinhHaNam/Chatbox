using System;
using System.Collections.Generic;

namespace SimsimiChat.Models;

public partial class RefreshToken
{
    public Guid Id { get; set; }

    public string Token { get; set; } = null!;

    public string JwtId { get; set; } = null!;

    public Guid UserId { get; set; }

    public bool IsUsed { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiryDate { get; set; }

    public virtual User User { get; set; } = null!;
}
