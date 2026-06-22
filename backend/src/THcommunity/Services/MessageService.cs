using THcommunity.Models;

namespace THcommunity.Services;

public interface IMessageService
{
    Task<IEnumerable<Message>> GetMessagesAsync(Guid teamId, int limit = 50, DateTime? before = null);
    Task<Message> SendAsync(Message message);
    Task<Message> UpdateAsync(Guid messageId, string content);
    Task DeleteAsync(Guid messageId);
    Task<MessageReaction> AddReactionAsync(Guid messageId, Guid userId, string emoji);
    Task RemoveReactionAsync(Guid messageId, Guid userId, string emoji);
    Task<IEnumerable<MessageReaction>> GetReactionsAsync(Guid messageId);
}

public class MessageService : IMessageService
{
    private readonly ISupabaseClient _db;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        ISupabaseClient db,
        IPushNotificationService pushService,
        ILogger<MessageService> logger)
    {
        _db = db;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(Guid teamId, int limit = 50, DateTime? before = null)
    {
        var query = $"team_id=eq.{teamId}&is_deleted=eq.false&order=created_at.desc&limit={limit}";
        
        if (before.HasValue)
        {
            query += $"&created_at=lt.{before.Value:o}";
        }
        
        return await _db.GetListAsync<Message>("messages", query);
    }

    public async Task<Message> SendAsync(Message message)
    {
        message.Id = Guid.NewGuid();
        message.CreatedAt = DateTime.UtcNow;
        message.IsDeleted = false;
        
        var sent = await _db.InsertAsync("messages", message);

        var sender = await _db.GetSingleAsync<User>("users", $"id=eq.{message.UserId}");
        var displayName = sender?.DisplayName ?? "Někdo";
        var preview = message.Content.Length > 50 
            ? message.Content[..50] + "..." 
            : message.Content;
        
        await _pushService.NotifyTeamAsync(message.TeamId, displayName, preview, message.UserId);

        return sent;
    }

    public async Task<Message> UpdateAsync(Guid messageId, string content)
    {
        return await _db.UpdateAsync<Message>("messages", $"id=eq.{messageId}", new 
        { 
            content,
            edited_at = DateTime.UtcNow 
        });
    }

    public async Task DeleteAsync(Guid messageId)
    {
        await _db.UpdateAsync<Message>("messages", $"id=eq.{messageId}", new 
        { 
            is_deleted = true 
        });
    }

    public async Task<MessageReaction> AddReactionAsync(Guid messageId, Guid userId, string emoji)
    {
        var existing = await _db.GetSingleAsync<MessageReaction>("message_reactions",
            $"message_id=eq.{messageId}&user_id=eq.{userId}&emoji=eq.{emoji}");
        
        if (existing != null)
            return existing;

        var reaction = new MessageReaction
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            UserId = userId,
            Emoji = emoji,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _db.InsertAsync("message_reactions", reaction);
    }

    public async Task RemoveReactionAsync(Guid messageId, Guid userId, string emoji)
    {
        await _db.DeleteAsync("message_reactions", 
            $"message_id=eq.{messageId}&user_id=eq.{userId}&emoji=eq.{emoji}");
    }

    public async Task<IEnumerable<MessageReaction>> GetReactionsAsync(Guid messageId)
    {
        return await _db.GetListAsync<MessageReaction>("message_reactions", $"message_id=eq.{messageId}");
    }
}
