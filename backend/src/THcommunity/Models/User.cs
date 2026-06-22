namespace THcommunity.Models;

public class User
{
    public Guid Id { get; set; }
    public string AuthId { get; set; } = string.Empty;
    public Guid? TeamId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Player;
    public PlayerPosition Position { get; set; } = PlayerPosition.Player;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum UserRole
{
    Admin,
    Coach,
    Player
}

public enum PlayerPosition
{
    Player,
    Goalie
}
