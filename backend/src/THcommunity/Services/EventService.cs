using THcommunity.Models;
using THcommunity.Configuration;
using Microsoft.Extensions.Options;

namespace THcommunity.Services;

public interface IEventService
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<IEnumerable<Event>> GetUpcomingAsync(Guid teamId, int limit = 20);
    Task<IEnumerable<Event>> GetPastAsync(Guid teamId, int limit = 20, int offset = 0);
    Task<Event> CreateAsync(Event evt);
    Task<Event> UpdateAsync(Event evt);
    Task DeleteAsync(Guid id);
    Task<EventResponse> RespondAsync(Guid eventId, Guid userId, ResponseType response, string? note);
    Task<IEnumerable<EventResponse>> GetResponsesAsync(Guid eventId);
    Task<IEnumerable<EventWaitlist>> GetWaitlistAsync(Guid eventId);
    Task<decimal> CalculatePriceAsync(Guid eventId, Guid userId);
}

public class EventService : IEventService
{
    private readonly ISupabaseClient _db;
    private readonly ITeamService _teamService;
    private readonly IPushNotificationService _pushService;
    private readonly TeamSettings _teamSettings;
    private readonly ILogger<EventService> _logger;

    public EventService(
        ISupabaseClient db,
        ITeamService teamService,
        IPushNotificationService pushService,
        IOptions<TeamSettings> teamSettings,
        ILogger<EventService> logger)
    {
        _db = db;
        _teamService = teamService;
        _pushService = pushService;
        _teamSettings = teamSettings.Value;
        _logger = logger;
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await _db.GetSingleAsync<Event>("events", $"id=eq.{id}");
    }

    public async Task<IEnumerable<Event>> GetUpcomingAsync(Guid teamId, int limit = 20)
    {
        var now = DateTime.UtcNow.ToString("o");
        return await _db.GetListAsync<Event>("events", 
            $"team_id=eq.{teamId}&start_time=gte.{now}&order=start_time.asc&limit={limit}");
    }

    public async Task<IEnumerable<Event>> GetPastAsync(Guid teamId, int limit = 20, int offset = 0)
    {
        var now = DateTime.UtcNow.ToString("o");
        return await _db.GetListAsync<Event>("events", 
            $"team_id=eq.{teamId}&start_time=lt.{now}&order=start_time.desc&limit={limit}&offset={offset}");
    }

    public async Task<Event> CreateAsync(Event evt)
    {
        evt.Id = Guid.NewGuid();
        evt.CapacityPlayers = evt.CapacityPlayers > 0 ? evt.CapacityPlayers : _teamSettings.Capacity.DefaultPlayers;
        evt.CapacityGoalies = evt.CapacityGoalies > 0 ? evt.CapacityGoalies : _teamSettings.Capacity.DefaultGoalies;
        evt.CreatedAt = DateTime.UtcNow;
        evt.UpdatedAt = DateTime.UtcNow;
        
        var createdEvent = await _db.InsertAsync("events", evt);

        // Send push notifications
        await _pushService.NotifyTeamAsync(evt.TeamId, "Nová událost", evt.Title);

        return createdEvent;
    }

    public async Task<Event> UpdateAsync(Event evt)
    {
        evt.UpdatedAt = DateTime.UtcNow;
        return await _db.UpdateAsync<Event>("events", $"id=eq.{evt.Id}", evt);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _db.DeleteAsync("events", $"id=eq.{id}");
    }

    public async Task<EventResponse> RespondAsync(Guid eventId, Guid userId, ResponseType response, string? note)
    {
        var evt = await GetByIdAsync(eventId);
        if (evt == null)
            throw new InvalidOperationException("Event not found");

        // Check existing response
        var existing = await _db.GetSingleAsync<EventResponse>("event_responses", 
            $"event_id=eq.{eventId}&user_id=eq.{userId}");

        var eventResponse = new EventResponse
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Response = response,
            Note = note,
            RespondedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (response == ResponseType.Cannot)
        {
            if (existing != null)
            {
                return await _db.UpdateAsync<EventResponse>("event_responses", 
                    $"id=eq.{existing.Id}", eventResponse);
            }
            return await _db.InsertAsync("event_responses", eventResponse);
        }

        // Check capacity
        var responses = await GetResponsesAsync(eventId);
        var isGoalie = response == ResponseType.Goalie;
        var currentCount = responses.Count(r => 
            (isGoalie && r.Response == ResponseType.Goalie) ||
            (!isGoalie && r.Response == ResponseType.Player));
        var capacity = isGoalie ? evt.CapacityGoalies : evt.CapacityPlayers;

        if (currentCount >= capacity && existing?.Response != response)
        {
            await AddToWaitlistAsync(eventId, userId, isGoalie);
        }

        if (existing != null)
        {
            return await _db.UpdateAsync<EventResponse>("event_responses", 
                $"id=eq.{existing.Id}", eventResponse);
        }
        return await _db.InsertAsync("event_responses", eventResponse);
    }

    public async Task<IEnumerable<EventResponse>> GetResponsesAsync(Guid eventId)
    {
        return await _db.GetListAsync<EventResponse>("event_responses", 
            $"event_id=eq.{eventId}&order=responded_at.asc");
    }

    public async Task<IEnumerable<EventWaitlist>> GetWaitlistAsync(Guid eventId)
    {
        return await _db.GetListAsync<EventWaitlist>("event_waitlist", 
            $"event_id=eq.{eventId}&order=position.asc");
    }

    public async Task<decimal> CalculatePriceAsync(Guid eventId, Guid userId)
    {
        var user = await _db.GetSingleAsync<User>("users", $"id=eq.{userId}");
        if (user == null) return 0;

        if (user.Position == PlayerPosition.Goalie)
            return _teamSettings.Pricing.GoaliePrice;

        var responses = (await GetResponsesAsync(eventId))
            .Where(r => r.Response == ResponseType.Player)
            .OrderBy(r => r.RespondedAt)
            .ToList();

        var position = responses.FindIndex(r => r.UserId == userId) + 1;
        
        if (position == 0) return _teamSettings.Pricing.Tier3Price;
        if (position <= _teamSettings.Pricing.Tier1Count) return _teamSettings.Pricing.Tier1Price;
        if (position <= _teamSettings.Pricing.Tier2Count) return _teamSettings.Pricing.Tier2Price;
        return _teamSettings.Pricing.Tier3Price;
    }

    private async Task AddToWaitlistAsync(Guid eventId, Guid userId, bool isGoalie)
    {
        var waitlist = await GetWaitlistAsync(eventId);
        var maxPosition = waitlist.Any() ? waitlist.Max(w => w.Position) : 0;

        var entry = new EventWaitlist
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Position = maxPosition + 1,
            IsGoalie = isGoalie,
            AddedAt = DateTime.UtcNow
        };

        await _db.InsertAsync("event_waitlist", entry);
    }
}
