using THcommunity.Configuration;
using THcommunity.Extensions;
using THcommunity.Services;
using Microsoft.Extensions.Options;

namespace THcommunity;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            IUserService userService,
            ITeamService teamService,
            IOptions<AppSettings> appSettings,
            HttpContext context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AuthEndpoints");

            var authId = context.User.GetUserId();
            logger.LogInformation("/api/auth/register called. AuthId={AuthId}, Request={Request}", authId, request);

            if (string.IsNullOrEmpty(authId))
            {
                logger.LogWarning("/api/auth/register unauthorized: missing auth id or token");
                return Results.Unauthorized();
            }

            var existingUser = await userService.GetByAuthIdAsync(authId);
            if (existingUser != null)
            {
                logger.LogInformation("/api/auth/register conflict: user already exists for authId={AuthId}", authId);
                return Results.Conflict(new { message = "Uživatel již existuje" });
            }

            // Optionally auto-assign new users to a configured default team.
            var defaultInviteCode = appSettings.Value.Registration.DefaultTeamInviteCode;
            Guid? defaultTeamId = null;
            if (!string.IsNullOrWhiteSpace(defaultInviteCode))
            {
                var defaultTeam = await teamService.GetByInviteCodeAsync(defaultInviteCode);
                defaultTeamId = defaultTeam?.Id;
            }

            var user = new Models.User
            {
                AuthId = authId,
                Email = context.User.GetEmail() ?? request.Email,
                Phone = request.Phone,
                DisplayName = request.DisplayName,
                Position = request.Position,
                TeamId = defaultTeamId
            };

            try
            {
                var createdUser = await userService.CreateAsync(user);
                logger.LogInformation("/api/auth/register success: created user id={UserId}", createdUser.Id);
                return Results.Created($"/api/users/{createdUser.Id}", createdUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "/api/auth/register failed for authId={AuthId}", authId);
                // Return a developer-friendly error in Development, otherwise generic error
                var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
                if (env.IsDevelopment())
                    return Results.Problem(detail: ex.ToString(), title: "Registration failed");
                return Results.Problem(title: "Registration failed");
            }
        })
        .RequireAuthorization();

        group.MapGet("/me", async (
            IUserService userService,
            HttpContext context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AuthEndpoints");

            var authId = context.User.GetUserId();
            logger.LogInformation("/api/auth/me called. AuthId={AuthId}", authId);

            if (string.IsNullOrEmpty(authId))
            {
                logger.LogWarning("/api/auth/me unauthorized: missing auth id or token");
                return Results.Unauthorized();
            }

            var user = await userService.GetByAuthIdAsync(authId);
            if (user == null)
            {
                logger.LogInformation("/api/auth/me: profile not found for authId={AuthId}", authId);
                return Results.NotFound();
            }

            logger.LogInformation("/api/auth/me: profile found userId={UserId}", user.Id);
            return Results.Ok(user);
        })
        .RequireAuthorization();

        // Development helper: return parsed claims so we can inspect token contents via Swagger
        group.MapGet("/debug/claims", (HttpContext context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AuthEndpoints");
            logger.LogInformation("/api/auth/debug/claims called");
            var claims = context.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Results.Ok(new { claims });
        })
        .RequireAuthorization();

        // Development helper: inspect the token and the corresponding user lookup result
        group.MapGet("/debug/inspect", async (IUserService userService, HttpContext context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AuthEndpoints");
            logger.LogInformation("/api/auth/debug/inspect called");

            var claims = context.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var authId = context.User.GetUserId();
            if (string.IsNullOrEmpty(authId))
            {
                logger.LogWarning("/api/auth/debug/inspect unauthorized: missing auth id");
                return Results.Unauthorized();
            }

            logger.LogInformation("/api/auth/debug/inspect: looking up user for authId={AuthId}", authId);
            var user = await userService.GetByAuthIdAsync(authId);
            return Results.Ok(new { authId, claims, user });
        })
        .RequireAuthorization();
    }
}

public record RegisterRequest(
    string Email,
    string Phone,
    string DisplayName,
    Models.PlayerPosition Position = Models.PlayerPosition.Player
);
