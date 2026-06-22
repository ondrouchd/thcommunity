using THcommunity.Extensions;
using THcommunity.Models;
using THcommunity.Services;

namespace THcommunity;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/messages")
            .WithTags("Messages")
            .RequireAuthorization();

        group.MapGet("/team/{teamId:guid}", async (
            Guid teamId,
            int? limit,
            DateTime? before,
            IMessageService messageService) =>
        {
            var messages = await messageService.GetMessagesAsync(teamId, limit ?? 50, before);
            return Results.Ok(messages);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMessageService messageService) =>
        {
            // Message details available through team messages list
            return Results.NotFound("Use /team/{teamId} endpoint to get messages");
        });

        group.MapPost("/", async (
            SendMessageRequest request,
            IMessageService messageService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || user.TeamId != request.TeamId)
                return Results.Forbid();

            var message = new Message
            {
                TeamId = request.TeamId,
                UserId = user.Id,
                Content = request.Content,
                Type = request.Type,
                MediaUrl = request.MediaUrl,
                MediaType = request.MediaType,
                ReplyToId = request.ReplyToId
            };

            var sentMessage = await messageService.SendAsync(message);
            return Results.Created($"/api/messages/{sentMessage.Id}", sentMessage);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateMessageRequest request,
            IMessageService messageService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            var updatedMessage = await messageService.UpdateAsync(id, request.Content);
            return Results.Ok(updatedMessage);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMessageService messageService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            await messageService.DeleteAsync(id);
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/reactions", async (Guid id, IMessageService messageService) =>
        {
            var reactions = await messageService.GetReactionsAsync(id);
            return Results.Ok(reactions);
        });

        group.MapPost("/{id:guid}/reactions", async (
            Guid id,
            AddReactionRequest request,
            IMessageService messageService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            var reaction = await messageService.AddReactionAsync(id, user.Id, request.Emoji);
            return Results.Ok(reaction);
        });

        group.MapDelete("/{messageId:guid}/reactions/{emoji}", async (
            Guid messageId,
            string emoji,
            IMessageService messageService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            await messageService.RemoveReactionAsync(messageId, user.Id, emoji);
            return Results.NoContent();
        });
    }
}

public record SendMessageRequest(
    Guid TeamId,
    string Content,
    MessageType Type = MessageType.Text,
    string? MediaUrl = null,
    string? MediaType = null,
    Guid? ReplyToId = null
);

public record UpdateMessageRequest(string Content);
public record AddReactionRequest(string Emoji);
