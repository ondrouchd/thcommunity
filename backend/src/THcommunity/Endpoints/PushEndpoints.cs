using THcommunity.Extensions;
using THcommunity.Services;

namespace THcommunity;

public static class PushEndpoints
{
    public static void MapPushEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/push")
            .WithTags("Push Notifications")
            .RequireAuthorization();

        group.MapPost("/subscribe", async (
            SubscribeRequest request,
            IPushNotificationService pushService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            await pushService.SubscribeAsync(user.Id, request.Endpoint, request.P256dh, request.Auth);
            return Results.Ok();
        });

        group.MapPost("/unsubscribe", async (
            UnsubscribeRequest request,
            IPushNotificationService pushService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            await pushService.UnsubscribeAsync(user.Id, request.Endpoint);
            return Results.Ok();
        });

        group.MapGet("/vapid-public-key", (IConfiguration configuration) =>
        {
            var publicKey = configuration["Vapid:PublicKey"];
            return Results.Ok(new { publicKey });
        });
    }
}

public record SubscribeRequest(string Endpoint, string P256dh, string Auth);
public record UnsubscribeRequest(string Endpoint);
