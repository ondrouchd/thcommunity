namespace THcommunity.Models;

public class Event
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EventType EventType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public int CapacityPlayers { get; set; }
    public int CapacityGoalies { get; set; }
    public DateTime? ResponseDeadline { get; set; }
    public decimal? PriceOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum EventType
{
    Training,
    Match
}

public class EventResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public ResponseType Response { get; set; }
    public string? Note { get; set; }
    public DateTime RespondedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum ResponseType
{
    Player,
    Goalie,
    Cannot,
    Maybe
}

public class EventWaitlist
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public int Position { get; set; }
    public bool IsGoalie { get; set; }
    public DateTime AddedAt { get; set; }
}
