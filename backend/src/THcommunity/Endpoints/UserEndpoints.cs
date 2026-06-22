using THcommunity.Extensions;
using THcommunity.Models;
using THcommunity.Services;

namespace THcommunity;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IUserService userService) =>
        {
            var user = await userService.GetByIdAsync(id);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        group.MapPut("/me", async (
            UpdateUserRequest request,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.NotFound();

            user.DisplayName = request.DisplayName ?? user.DisplayName;
            user.Phone = request.Phone ?? user.Phone;
            user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
            user.Position = request.Position ?? user.Position;

            var updatedUser = await userService.UpdateAsync(user);
            return Results.Ok(updatedUser);
        });

        group.MapGet("/me/statistics", async (
            IStatisticsService statisticsService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.NotFound();

            var stats = await statisticsService.GetUserStatisticsAsync(user.Id);
            return Results.Ok(stats);
        });

        group.MapGet("/team/{teamId:guid}/statistics", async (
            Guid teamId,
            DateTime? from,
            DateTime? to,
            IStatisticsService statisticsService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, teamId))
                return Results.Forbid();

            var stats = await statisticsService.GetTeamMemberStatisticsAsync(teamId, from, to);
            return Results.Ok(stats);
        });
    }
}

public record UpdateUserRequest(
    string? DisplayName,
    string? Phone,
    string? AvatarUrl,
    PlayerPosition? Position
);
