using THcommunity.Extensions;
using THcommunity.Models;
using THcommunity.Services;

namespace THcommunity;

public static class SurveyEndpoints
{
    public static void MapSurveyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/surveys")
            .WithTags("Surveys")
            .RequireAuthorization();

        group.MapGet("/team/{teamId:guid}", async (
            Guid teamId,
            ISurveyService surveyService) =>
        {
            var surveys = await surveyService.GetActiveAsync(teamId);
            return Results.Ok(surveys);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISurveyService surveyService) =>
        {
            var survey = await surveyService.GetByIdAsync(id);
            return survey is null ? Results.NotFound() : Results.Ok(survey);
        });

        group.MapGet("/{id:guid}/options", async (Guid id, ISurveyService surveyService) =>
        {
            var options = await surveyService.GetOptionsAsync(id);
            return Results.Ok(options);
        });

        group.MapGet("/{id:guid}/results", async (
            Guid id,
            ISurveyService surveyService) =>
        {
            var survey = await surveyService.GetByIdAsync(id);
            if (survey == null)
                return Results.NotFound();

            var options = await surveyService.GetOptionsAsync(id);
            var votes = await surveyService.GetVotesAsync(id);
            var voteCounts = options.ToDictionary(o => o.Id, o => votes.Count(v => v.OptionId == o.Id));

            return Results.Ok(new
            {
                survey,
                options,
                voteCounts,
                votes = survey.IsAnonymous ? null : votes
            });
        });

        group.MapPost("/", async (
            CreateSurveyRequest request,
            ISurveyService surveyService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || user.TeamId != request.TeamId)
                return Results.Forbid();

            // Only Admin/Coach can create surveys
            if (!await userService.IsAdminOrCoachAsync(user.Id, request.TeamId))
                return Results.Forbid();

            var survey = new Survey
            {
                TeamId = request.TeamId,
                CreatedByUserId = user.Id,
                Question = request.Question,
                AllowMultipleAnswers = request.AllowMultipleAnswers,
                IsAnonymous = request.IsAnonymous,
                ExpiresAt = request.ExpiresAt
            };

            var createdSurvey = await surveyService.CreateAsync(survey, request.Options.ToList());
            return Results.Created($"/api/surveys/{createdSurvey.Id}", createdSurvey);
        });

        group.MapPost("/{id:guid}/vote", async (
            Guid id,
            VoteRequest request,
            ISurveyService surveyService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            var vote = await surveyService.VoteAsync(request.OptionId, user.Id);
            return Results.Ok(vote);
        });

        group.MapDelete("/{surveyId:guid}/vote/{optionId:guid}", async (
            Guid surveyId,
            Guid optionId,
            ISurveyService surveyService,
            IUserService userService,
            HttpContext context) =>
        {
            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null)
                return Results.Unauthorized();

            await surveyService.RemoveVoteAsync(optionId, user.Id);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/close", async (
            Guid id,
            ISurveyService surveyService,
            IUserService userService,
            HttpContext context) =>
        {
            var survey = await surveyService.GetByIdAsync(id);
            if (survey == null)
                return Results.NotFound();

            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, survey.TeamId))
                return Results.Forbid();

            var closedSurvey = await surveyService.CloseAsync(id);
            return Results.Ok(closedSurvey);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISurveyService surveyService,
            IUserService userService,
            HttpContext context) =>
        {
            var survey = await surveyService.GetByIdAsync(id);
            if (survey == null)
                return Results.NotFound();

            var authId = context.User.GetUserId();
            var user = await userService.GetByAuthIdAsync(authId!);
            
            if (user == null || !await userService.IsAdminOrCoachAsync(user.Id, survey.TeamId))
                return Results.Forbid();

            await surveyService.DeleteAsync(id);
            return Results.NoContent();
        });
    }
}

public record CreateSurveyRequest(
    Guid TeamId,
    string Question,
    IEnumerable<string> Options,
    bool AllowMultipleAnswers = false,
    bool IsAnonymous = false,
    DateTime? ExpiresAt = null
);

public record VoteRequest(Guid OptionId);
