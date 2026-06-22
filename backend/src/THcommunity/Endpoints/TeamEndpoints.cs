using THcommunity.Extensions;
using THcommunity.Models;
using THcommunity.Services;

namespace THcommunity;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/teams")
            .WithTags("Teams")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ITeamService teamService) =>
        {
            var team = await teamService.GetByIdAsync(id);
            return team is null ? Results.NotFound() : Results.Ok(team);
        });

        // Team creation disabled - using single team model
        // group.MapPost("/", async (...) => { ... });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTeamRequest request,
            ITeamService teamService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, id))
                return Results.Forbid();

            var team = await teamService.GetByIdAsync(id);
            if (team == null)
                return Results.NotFound();

            team.Name = request.Name ?? team.Name;
            team.Description = request.Description ?? team.Description;

            var updatedTeam = await teamService.UpdateAsync(team);
            return Results.Ok(updatedTeam);
        });

        group.MapGet("/{id:guid}/members", async (
            Guid id,
            IUserService userService) =>
        {
            var members = await userService.GetTeamMembersAsync(id);
            return Results.Ok(members);
        });

        // Join team disabled - using single team model  
        // group.MapPost("/join/{inviteCode}", async (...) => { ... });

        group.MapPost("/{id:guid}/leave", async (
            Guid id,
            ITeamService teamService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            await teamService.LeaveTeamAsync(user.Id, id);
            return Results.Ok();
        });

        group.MapGet("/{id:guid}/settings", async (
            Guid id,
            ITeamService teamService) =>
        {
            var settings = await teamService.GetSettingsAsync(id);
            return settings is null ? Results.NotFound() : Results.Ok(settings);
        });

        group.MapPut("/{id:guid}/settings", async (
            Guid id,
            TeamSettingsEntity settings,
            ITeamService teamService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || user.Role != UserRole.Admin)
                return Results.Forbid();

            settings.TeamId = id;
            var updatedSettings = await teamService.UpdateSettingsAsync(settings);
            return Results.Ok(updatedSettings);
        });

        group.MapGet("/{id:guid}/join-requests", async (
            Guid id,
            ITeamService teamService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, id))
                return Results.Forbid();

            var requests = await teamService.GetPendingRequestsAsync(id);
            return Results.Ok(requests);
        });

        group.MapPost("/{teamId:guid}/join-requests/{requestId:guid}/process", async (
            Guid teamId,
            Guid requestId,
            ProcessJoinRequestRequest request,
            ITeamService teamService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, teamId))
                return Results.Forbid();

            await teamService.ProcessJoinRequestAsync(requestId, request.Approve, user.Id);
            return Results.Ok();
        });

        group.MapPut("/{teamId:guid}/members/{userId:guid}/role", async (
            Guid teamId,
            Guid userId,
            UpdateRoleRequest request,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var currentUser = await userService.GetByAuthIdAsync(authId!);
            
            if (currentUser == null || currentUser.Role != UserRole.Admin || currentUser.TeamId != teamId)
                return Results.Forbid();

            var targetUser = await userService.GetByIdAsync(userId);
            if (targetUser == null || targetUser.TeamId != teamId)
                return Results.NotFound();

            targetUser.Role = request.Role;
            await userService.UpdateAsync(targetUser);
            return Results.Ok(targetUser);
        });
    }
}

public record CreateTeamRequest(string Name, string? Description);
public record UpdateTeamRequest(string? Name, string? Description);
public record ProcessJoinRequestRequest(bool Approve);
public record UpdateRoleRequest(UserRole Role);
