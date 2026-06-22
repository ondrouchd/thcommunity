using THcommunity.Configuration;
using THcommunity.Models;
using WebPush;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace THcommunity.Services;

public interface IPushNotificationService
{
    Task<Models.PushSubscription> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth);
    Task UnsubscribeAsync(Guid userId, string endpoint);
    Task NotifyUserAsync(Guid userId, string title, string body, string? url = null);
    Task NotifyTeamAsync(Guid teamId, string title, string body, Guid? excludeUserId = null);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly ISupabaseClient _db;
    private readonly WebPushClient _webPush;
    private readonly VapidDetails _vapidDetails;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        ISupabaseClient db,
        IOptions<AppSettings> settings,
        ILogger<PushNotificationService> logger)
    {
        _db = db;
        _logger = logger;
        
        var webPushSettings = settings.Value.WebPush;
        _vapidDetails = new VapidDetails(
            webPushSettings.Subject,
            webPushSettings.VapidPublicKey,
            webPushSettings.VapidPrivateKey);
        
        _webPush = new WebPushClient();
    }

    public async Task<Models.PushSubscription> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth)
    {
        await _db.DeleteAsync("push_subscriptions", $"endpoint=eq.{Uri.EscapeDataString(endpoint)}");

        var subscription = new Models.PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            CreatedAt = DateTime.UtcNow
        };

        return await _db.InsertAsync("push_subscriptions", subscription);
    }

    public async Task UnsubscribeAsync(Guid userId, string endpoint)
    {
        await _db.DeleteAsync("push_subscriptions", 
            $"user_id=eq.{userId}&endpoint=eq.{Uri.EscapeDataString(endpoint)}");
    }

    public async Task NotifyUserAsync(Guid userId, string title, string body, string? url = null)
    {
        var subscriptions = await _db.GetListAsync<Models.PushSubscription>("push_subscriptions", $"user_id=eq.{userId}");
        
        foreach (var sub in subscriptions)
        {
            await SendNotificationAsync(sub, title, body, url);
        }
    }

    public async Task NotifyTeamAsync(Guid teamId, string title, string body, Guid? excludeUserId = null)
    {
        var users = await _db.GetListAsync<User>("users", $"team_id=eq.{teamId}");
        
        foreach (var user in users)
        {
            if (excludeUserId.HasValue && user.Id == excludeUserId.Value)
                continue;

            var subscriptions = await _db.GetListAsync<Models.PushSubscription>("push_subscriptions", $"user_id=eq.{user.Id}");
            
            foreach (var sub in subscriptions)
            {
                await SendNotificationAsync(sub, title, body);
            }
        }
    }

    private async Task SendNotificationAsync(Models.PushSubscription subscription, string title, string body, string? url = null)
    {
        try
        {
            var webPushSubscription = new WebPush.PushSubscription(
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth);

            var payload = JsonSerializer.Serialize(new
            {
                title,
                body,
                url = url ?? "/",
                icon = "/icon-192x192.png"
            });

            await _webPush.SendNotificationAsync(webPushSubscription, payload, _vapidDetails);
        }
        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            await _db.DeleteAsync("push_subscriptions", $"id=eq.{subscription.Id}");
            _logger.LogInformation("Removed expired push subscription {Id}", subscription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to {Endpoint}", subscription.Endpoint);
        }
    }
}
