namespace THcommunity.Models;

public class Survey
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool AllowMultipleAnswers { get; set; }
    public bool IsAnonymous { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsClosed { get; set; }
}

public class SurveyOption
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class SurveyVote
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public Guid OptionId { get; set; }
    public Guid UserId { get; set; }
    public DateTime VotedAt { get; set; }
}
