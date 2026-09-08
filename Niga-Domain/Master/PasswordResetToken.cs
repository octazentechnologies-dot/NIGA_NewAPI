using System;

namespace Niga_Domain.Master;

/// <summary>SEC-02.01 — Password reset token (hashed). Manual SQL: M01_Foundation_Security_CreateTables.sql</summary>
public partial class PasswordResetToken
{
    public long PasswordResetTokenId { get; set; }

    public long UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
