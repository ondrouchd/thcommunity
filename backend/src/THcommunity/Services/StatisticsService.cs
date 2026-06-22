using THcommunity.Models;

namespace THcommunity.Services;

public interface IStatisticsService
{
    Task<TeamStatistics> GetTeamStatisticsAsync(Guid teamId);
    Task<UserStatistics> GetUserStatisticsAsync(Guid userId);
    Task<EventStatistics> GetEventStatisticsAsync(Guid eventId);
    Task<IEnumerable<MemberStatistics>> GetTeamMemberStatisticsAsync(Guid teamId, DateTime? from, DateTime? to);
}

public class StatisticsService : IStatisticsService
{
    private readonly ISupabaseClient _db;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(ISupabaseClient db, ILogger<StatisticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TeamStatistics> GetTeamStatisticsAsync(Guid teamId)
    {
        var members = await _db.GetListAsync<User>("users", $"team_id=eq.{teamId}");
        
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30).ToString("o");
        
        var events = await _db.GetListAsync<Event>("events", 
            $"team_id=eq.{teamId}&start_time=gte.{thirtyDaysAgo}&start_time=lt.{now:o}");
        
        var eventIds = string.Join(",", events.Select(e => e.Id));
        var responses = eventIds.Length > 0 
            ? await _db.GetListAsync<EventResponse>("event_responses", $"event_id=in.({eventIds})")
            : new List<EventResponse>();

        var attendedResponses = responses.Where(r => 
            r.Response == ResponseType.Player || r.Response == ResponseType.Goalie).ToList();

        return new TeamStatistics
        {
            TotalMembers = members.Count,
            TotalEvents = events.Count,
            AverageAttendance = events.Count > 0 
                ? (double)attendedResponses.Count / events.Count 
                : 0,
            PlayersByPosition = new Dictionary<string, int>
            {
                { "Player", members.Count(m => m.Position == PlayerPosition.Player) },
                { "Goalie", members.Count(m => m.Position == PlayerPosition.Goalie) }
            }
        };
    }

    public async Task<UserStatistics> GetUserStatisticsAsync(Guid userId)
    {
        var user = await _db.GetSingleAsync<User>("users", $"id=eq.{userId}");
        if (user?.TeamId == null)
        {
            return new UserStatistics
            {
                TotalEventsAttended = 0,
                AttendanceRate = 0,
                TotalPayments = 0
            };
        }

        var now = DateTime.UtcNow;
        var ninetyDaysAgo = now.AddDays(-90).ToString("o");
        
        var events = await _db.GetListAsync<Event>("events", 
            $"team_id=eq.{user.TeamId}&start_time=gte.{ninetyDaysAgo}&start_time=lt.{now:o}");
        
        var eventIds = string.Join(",", events.Select(e => e.Id));
        var userResponses = eventIds.Length > 0
            ? await _db.GetListAsync<EventResponse>("event_responses", $"user_id=eq.{userId}&event_id=in.({eventIds})")
            : new List<EventResponse>();

        var attended = userResponses.Count(r => 
            r.Response == ResponseType.Player || r.Response == ResponseType.Goalie);

        return new UserStatistics
        {
            TotalEventsAttended = attended,
            AttendanceRate = events.Count > 0 ? (double)attended / events.Count * 100 : 0,
            TotalPayments = 0
        };
    }

    public async Task<EventStatistics> GetEventStatisticsAsync(Guid eventId)
    {
        var responses = await _db.GetListAsync<EventResponse>("event_responses", $"event_id=eq.{eventId}");
        
        return new EventStatistics
        {
            TotalResponses = responses.Count,
            PlayersConfirmed = responses.Count(r => r.Response == ResponseType.Player),
            GoaliesConfirmed = responses.Count(r => r.Response == ResponseType.Goalie),
            Declined = responses.Count(r => r.Response == ResponseType.Cannot),
            Pending = responses.Count(r => r.Response == ResponseType.Maybe)
        };
    }

    public async Task<IEnumerable<MemberStatistics>> GetTeamMemberStatisticsAsync(Guid teamId, DateTime? from, DateTime? to)
    {
        var members = await _db.GetListAsync<User>("users", $"team_id=eq.{teamId}");
        var result = new List<MemberStatistics>();
        
        var fromDate = (from ?? DateTime.UtcNow.AddDays(-90)).ToString("o");
        var toDate = (to ?? DateTime.UtcNow).ToString("o");
        
        var events = await _db.GetListAsync<Event>("events", 
            $"team_id=eq.{teamId}&start_time=gte.{fromDate}&start_time=lt.{toDate}");
        
        var eventIds = string.Join(",", events.Select(e => e.Id));
        var allResponses = eventIds.Length > 0
            ? await _db.GetListAsync<EventResponse>("event_responses", $"event_id=in.({eventIds})")
            : new List<EventResponse>();

        foreach (var member in members)
        {
            var memberResponses = allResponses.Where(r => r.UserId == member.Id).ToList();
            var attended = memberResponses.Count(r => 
                r.Response == ResponseType.Player || r.Response == ResponseType.Goalie);
            
            result.Add(new MemberStatistics
            {
                UserId = member.Id,
                DisplayName = member.DisplayName,
                TotalEventsAttended = attended,
                AttendanceRate = events.Count > 0 ? (double)attended / events.Count * 100 : 0
            });
        }

        return result.OrderByDescending(m => m.AttendanceRate);
    }
}

public class TeamStatistics
{
    public int TotalMembers { get; set; }
    public int TotalEvents { get; set; }
    public double AverageAttendance { get; set; }
    public Dictionary<string, int> PlayersByPosition { get; set; } = new();
}

public class UserStatistics
{
    public int TotalEventsAttended { get; set; }
    public double AttendanceRate { get; set; }
    public decimal TotalPayments { get; set; }
}

public class EventStatistics
{
    public int TotalResponses { get; set; }
    public int PlayersConfirmed { get; set; }
    public int GoaliesConfirmed { get; set; }
    public int Declined { get; set; }
    public int Pending { get; set; }
}

public class MemberStatistics
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int TotalEventsAttended { get; set; }
    public double AttendanceRate { get; set; }
}
