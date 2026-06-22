namespace THcommunity.Models;

public class UserStatistics
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int AttendedEvents { get; set; }
    public int MissedEvents { get; set; }
    public double AttendancePercentage => TotalEvents > 0 ? (double)AttendedEvents / TotalEvents * 100 : 0;
    public int TrainingsAttended { get; set; }
    public int MatchesAttended { get; set; }
    public decimal TotalPaid { get; set; }
}

public class EventStatistics
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public DateTime StartTime { get; set; }
    public int PlayersAttending { get; set; }
    public int GoaliesAttending { get; set; }
    public int CannotAttend { get; set; }
    public int NoResponse { get; set; }
    public int WaitlistCount { get; set; }
}

public class TeamStatistics
{
    public Guid TeamId { get; set; }
    public int TotalMembers { get; set; }
    public int TotalEvents { get; set; }
    public double AverageAttendance { get; set; }
    public int ActivePlayersLast30Days { get; set; }
}
