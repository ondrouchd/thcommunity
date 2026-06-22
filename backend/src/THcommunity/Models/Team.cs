namespace THcommunity.Models;

using System.Text.Json;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TeamSettingsEntity
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public JsonElement? Pricing { get; set; }
    public JsonElement? Capacity { get; set; }
    public JsonElement? Notifications { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TeamMember
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Player";
    public string Position { get; set; } = "Player";
    public DateTime JoinedAt { get; set; }
}
