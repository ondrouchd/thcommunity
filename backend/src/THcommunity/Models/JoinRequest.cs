namespace THcommunity.Models;

public class JoinRequest
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string? Message { get; set; }
    public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;
    public Guid? ProcessedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public enum JoinRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class Invite
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int MaxUses { get; set; } = 1;
    public int UsedCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
