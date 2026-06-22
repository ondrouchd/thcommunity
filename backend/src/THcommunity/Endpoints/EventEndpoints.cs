using THcommunity.Extensions;
using THcommunity.Models;
using THcommunity.Services;

namespace THcommunity;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events")
            .WithTags("Events")
            .RequireAuthorization();

        group.MapGet("/team/{teamId:guid}/upcoming", async (
            Guid teamId,
            int? limit,
            IEventService eventService) =>
        {
            var events = await eventService.GetUpcomingAsync(teamId, limit ?? 20);
            return Results.Ok(events);
        });

        group.MapGet("/team/{teamId:guid}/past", async (
            Guid teamId,
            int? limit,
            int? offset,
            IEventService eventService) =>
        {
            var events = await eventService.GetPastAsync(teamId, limit ?? 20, offset ?? 0);
            return Results.Ok(events);
        });

        group.MapGet("/{id:guid}", async (Guid id, IEventService eventService) =>
        {
            var evt = await eventService.GetByIdAsync(id);
            return evt is null ? Results.NotFound() : Results.Ok(evt);
        });

        group.MapPost("/", async (
            CreateEventRequest request,
            IEventService eventService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, request.TeamId))
                return Results.Forbid();

            var evt = new Event
            {
                TeamId = request.TeamId,
                CreatedByUserId = user.Id,
                Title = request.Title,
                Description = request.Description,
                EventType = request.EventType,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Location = request.Location,
                CapacityPlayers = request.CapacityPlayers ?? 0,
                CapacityGoalies = request.CapacityGoalies ?? 0,
                ResponseDeadline = request.ResponseDeadline,
                PriceOverride = request.PriceOverride
            };

            var createdEvent = await eventService.CreateAsync(evt);
            return Results.Created($"/api/events/{createdEvent.Id}", createdEvent);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateEventRequest request,
            IEventService eventService,
            IUserService userService,
            HttpContext context) =>
        {
            var evt = await eventService.GetByIdAsync(id);
            if (evt == null)
                return Results.NotFound();

            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, evt.TeamId))
                return Results.Forbid();

            evt.Title = request.Title ?? evt.Title;
            evt.Description = request.Description ?? evt.Description;
            evt.StartTime = request.StartTime ?? evt.StartTime;
            evt.EndTime = request.EndTime ?? evt.EndTime;
            evt.Location = request.Location ?? evt.Location;
            evt.CapacityPlayers = request.CapacityPlayers ?? evt.CapacityPlayers;
            evt.CapacityGoalies = request.CapacityGoalies ?? evt.CapacityGoalies;
            evt.ResponseDeadline = request.ResponseDeadline ?? evt.ResponseDeadline;
            evt.PriceOverride = request.PriceOverride ?? evt.PriceOverride;

            var updatedEvent = await eventService.UpdateAsync(evt);
            return Results.Ok(updatedEvent);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IEventService eventService,
            IUserService userService,
            HttpContext context) =>
        {
            var evt = await eventService.GetByIdAsync(id);
            if (evt == null)
                return Results.NotFound();

            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, evt.TeamId))
                return Results.Forbid();

            await eventService.DeleteAsync(id);
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/responses", async (Guid id, IEventService eventService) =>
        {
            var responses = await eventService.GetResponsesAsync(id);
            return Results.Ok(responses);
        });

        group.MapPost("/{id:guid}/respond", async (
            Guid id,
            RespondRequest request,
            IEventService eventService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            var response = await eventService.RespondAsync(id, user.Id, request.Response, request.Note);
            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}/my-response", async (
            Guid id,
            IEventService eventService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            var responses = await eventService.GetResponsesAsync(id);
            var response = responses.FirstOrDefault(r => r.UserId == user.Id);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapGet("/{id:guid}/waitlist", async (Guid id, IEventService eventService) =>
        {
            var waitlist = await eventService.GetWaitlistAsync(id);
            return Results.Ok(waitlist);
        });

        group.MapGet("/{id:guid}/price", async (
            Guid id,
            IEventService eventService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            var price = await eventService.CalculatePriceAsync(id, user.Id);
            return Results.Ok(new { price });
        });

        group.MapGet("/{id:guid}/statistics", async (
            Guid id,
            IStatisticsService statisticsService) =>
        {
            var stats = await statisticsService.GetEventStatisticsAsync(id);
            return Results.Ok(stats);
        });
    }
}

public record CreateEventRequest(
    Guid TeamId,
    string Title,
    string? Description,
    EventType EventType,
    DateTime StartTime,
    DateTime EndTime,
    string? Location,
    int? CapacityPlayers,
    int? CapacityGoalies,
    DateTime? ResponseDeadline,
    decimal? PriceOverride
);

public record UpdateEventRequest(
    string? Title,
    string? Description,
    DateTime? StartTime,
    DateTime? EndTime,
    string? Location,
    int? CapacityPlayers,
    int? CapacityGoalies,
    DateTime? ResponseDeadline,
    decimal? PriceOverride
);

public record RespondRequest(ResponseType Response, string? Note);
