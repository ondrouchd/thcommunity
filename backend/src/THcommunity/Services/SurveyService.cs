using THcommunity.Models;

namespace THcommunity.Services;

public interface ISurveyService
{
    Task<Survey?> GetByIdAsync(Guid id);
    Task<IEnumerable<Survey>> GetActiveAsync(Guid teamId);
    Task<Survey> CreateAsync(Survey survey, List<string> options);
    Task<Survey> CloseAsync(Guid surveyId);
    Task DeleteAsync(Guid surveyId);
    Task<SurveyVote> VoteAsync(Guid optionId, Guid userId);
    Task RemoveVoteAsync(Guid optionId, Guid userId);
    Task<IEnumerable<SurveyOption>> GetOptionsAsync(Guid surveyId);
    Task<IEnumerable<SurveyVote>> GetVotesAsync(Guid surveyId);
}

public class SurveyService : ISurveyService
{
    private readonly ISupabaseClient _db;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(
        ISupabaseClient db,
        IPushNotificationService pushService,
        ILogger<SurveyService> logger)
    {
        _db = db;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<Survey?> GetByIdAsync(Guid id)
    {
        return await _db.GetSingleAsync<Survey>("surveys", $"id=eq.{id}");
    }

    public async Task<IEnumerable<Survey>> GetActiveAsync(Guid teamId)
    {
        return await _db.GetListAsync<Survey>("surveys", 
            $"team_id=eq.{teamId}&is_closed=eq.false&order=created_at.desc");
    }

    public async Task<Survey> CreateAsync(Survey survey, List<string> options)
    {
        survey.Id = Guid.NewGuid();
        survey.CreatedAt = DateTime.UtcNow;
        survey.IsClosed = false;
        
        var created = await _db.InsertAsync("surveys", survey);

        foreach (var optionText in options)
        {
            var option = new SurveyOption
            {
                Id = Guid.NewGuid(),
                SurveyId = created.Id,
                Text = optionText
            };
            await _db.InsertAsync("survey_options", option);
        }

        await _pushService.NotifyTeamAsync(survey.TeamId, "Nová anketa", survey.Question);

        return created;
    }

    public async Task<Survey> CloseAsync(Guid surveyId)
    {
        return await _db.UpdateAsync<Survey>("surveys", $"id=eq.{surveyId}", new 
        { 
            is_closed = true 
        });
    }

    public async Task<SurveyVote> VoteAsync(Guid optionId, Guid userId)
    {
        var option = await _db.GetSingleAsync<SurveyOption>("survey_options", $"id=eq.{optionId}");
        if (option == null)
            throw new InvalidOperationException("Option not found");

        var survey = await GetByIdAsync(option.SurveyId);
        if (survey == null || survey.IsClosed)
            throw new InvalidOperationException("Survey is closed");

        if (!survey.AllowMultipleAnswers)
        {
            var surveyOptions = await GetOptionsAsync(survey.Id);
            var optionIds = string.Join(",", surveyOptions.Select(o => o.Id));
            var existingVotes = await _db.GetListAsync<SurveyVote>("survey_votes", 
                $"user_id=eq.{userId}&option_id=in.({optionIds})");
            
            foreach (var vote in existingVotes)
            {
                await _db.DeleteAsync("survey_votes", $"id=eq.{vote.Id}");
            }
        }

        var newVote = new SurveyVote
        {
            Id = Guid.NewGuid(),
            OptionId = optionId,
            UserId = userId,
            VotedAt = DateTime.UtcNow
        };

        return await _db.InsertAsync("survey_votes", newVote);
    }

    public async Task RemoveVoteAsync(Guid optionId, Guid userId)
    {
        await _db.DeleteAsync("survey_votes", $"option_id=eq.{optionId}&user_id=eq.{userId}");
    }

    public async Task DeleteAsync(Guid surveyId)
    {
        // Delete votes first
        var options = await GetOptionsAsync(surveyId);
        var optionIds = string.Join(",", options.Select(o => o.Id));
        
        if (!string.IsNullOrEmpty(optionIds))
        {
            await _db.DeleteAsync("survey_votes", $"option_id=in.({optionIds})");
        }
        
        // Delete options
        await _db.DeleteAsync("survey_options", $"survey_id=eq.{surveyId}");
        
        // Delete survey
        await _db.DeleteAsync("surveys", $"id=eq.{surveyId}");
    }

    public async Task<IEnumerable<SurveyOption>> GetOptionsAsync(Guid surveyId)
    {
        return await _db.GetListAsync<SurveyOption>("survey_options", $"survey_id=eq.{surveyId}");
    }

    public async Task<IEnumerable<SurveyVote>> GetVotesAsync(Guid surveyId)
    {
        var options = await GetOptionsAsync(surveyId);
        var optionIds = string.Join(",", options.Select(o => o.Id));
        
        if (string.IsNullOrEmpty(optionIds))
            return Enumerable.Empty<SurveyVote>();
            
        return await _db.GetListAsync<SurveyVote>("survey_votes", $"option_id=in.({optionIds})");
    }
}
