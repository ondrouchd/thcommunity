using THcommunity.Models;

namespace THcommunity.Services;

public interface ITeamService
{
    Task<Team?> GetByIdAsync(Guid id);
    Task<Team?> GetByInviteCodeAsync(string inviteCode);
    Task<Team> CreateAsync(Team team, Guid creatorUserId);
    Task<Team> UpdateAsync(Team team);
    Task<TeamSettingsEntity?> GetSettingsAsync(Guid teamId);
    Task<TeamSettingsEntity> UpdateSettingsAsync(TeamSettingsEntity settings);
    Task<IEnumerable<User>> GetMembersAsync(Guid teamId);
    Task AddMemberAsync(Guid teamId, Guid userId);
    Task RemoveMemberAsync(Guid teamId, Guid userId);
    Task JoinTeamAsync(Guid userId, Guid teamId);
    Task LeaveTeamAsync(Guid userId, Guid teamId);
    Task<JoinRequest> CreateJoinRequestAsync(Guid teamId, Guid userId, string? message);
    Task<IEnumerable<JoinRequest>> GetPendingRequestsAsync(Guid teamId);
    Task ProcessJoinRequestAsync(Guid requestId, bool approve, Guid processedByUserId);
    Task ApproveJoinRequestAsync(Guid requestId);
    Task RejectJoinRequestAsync(Guid requestId);
}

public class TeamService : ITeamService
{
    private readonly ISupabaseClient _db;
    private readonly ILogger<TeamService> _logger;

    public TeamService(ISupabaseClient db, ILogger<TeamService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Team?> GetByIdAsync(Guid id)
    {
        return await _db.GetSingleAsync<Team>("teams", $"id=eq.{id}");
    }

    public async Task<Team?> GetByInviteCodeAsync(string inviteCode)
    {
        return await _db.GetSingleAsync<Team>("teams", $"invite_code=eq.{inviteCode}");
    }

    public async Task<Team> CreateAsync(Team team, Guid creatorUserId)
    {
        team.Id = Guid.NewGuid();
        team.InviteCode = GenerateInviteCode();
        team.CreatedAt = DateTime.UtcNow;
        team.UpdatedAt = DateTime.UtcNow;
        
        var createdTeam = await _db.InsertAsync("teams", team);

        // Create default settings row; the database fills pricing/capacity/notifications
        // from its column defaults when those keys are omitted from the insert.
        await _db.InsertAsync("team_settings", new TeamSettingsEntity
        {
            Id = Guid.NewGuid(),
            TeamId = createdTeam.Id
        });

        // Update creator as admin
        await _db.UpdateAsync<User>("users", $"id=eq.{creatorUserId}", new 
        { 
            team_id = createdTeam.Id, 
            role = "Admin",
            updated_at = DateTime.UtcNow 
        });

        return createdTeam;
    }

    public async Task<Team> UpdateAsync(Team team)
    {
        team.UpdatedAt = DateTime.UtcNow;
        return await _db.UpdateAsync<Team>("teams", $"id=eq.{team.Id}", team);
    }

    public async Task<TeamSettingsEntity?> GetSettingsAsync(Guid teamId)
    {
        var settings = await _db.GetSingleAsync<TeamSettingsEntity>("team_settings", $"team_id=eq.{teamId}");
        if (settings != null)
        {
            return settings;
        }

        // Lazily provision default settings for teams created before settings existed.
        return await _db.InsertAsync("team_settings", new TeamSettingsEntity
        {
            Id = Guid.NewGuid(),
            TeamId = teamId
        });
    }

    public async Task<TeamSettingsEntity> UpdateSettingsAsync(TeamSettingsEntity settings)
    {
        return await _db.UpdateAsync<TeamSettingsEntity>("team_settings", $"id=eq.{settings.Id}", settings);
    }

    public async Task<IEnumerable<User>> GetMembersAsync(Guid teamId)
    {
        return await _db.GetListAsync<User>("users", $"team_id=eq.{teamId}&order=role,display_name");
    }

    public async Task AddMemberAsync(Guid teamId, Guid userId)
    {
        await _db.UpdateAsync<User>("users", $"id=eq.{userId}", new 
        { 
            team_id = teamId,
            updated_at = DateTime.UtcNow 
        });
    }

    public async Task RemoveMemberAsync(Guid teamId, Guid userId)
    {
        await _db.UpdateAsync<User>("users", $"id=eq.{userId}", new 
        { 
            team_id = (Guid?)null,
            role = "Player",
            updated_at = DateTime.UtcNow 
        });
    }

    public async Task JoinTeamAsync(Guid userId, Guid teamId)
    {
        await AddMemberAsync(teamId, userId);
    }

    public async Task LeaveTeamAsync(Guid userId, Guid teamId)
    {
        await RemoveMemberAsync(teamId, userId);
    }

    public async Task<JoinRequest> CreateJoinRequestAsync(Guid teamId, Guid userId, string? message)
    {
        var request = new JoinRequest
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = userId,
            Message = message,
            Status = JoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _db.InsertAsync("join_requests", request);
    }

    public async Task<IEnumerable<JoinRequest>> GetPendingRequestsAsync(Guid teamId)
    {
        return await _db.GetListAsync<JoinRequest>("join_requests", $"team_id=eq.{teamId}&status=eq.Pending");
    }

    public async Task<IEnumerable<JoinRequest>> GetPendingJoinRequestsAsync(Guid teamId)
    {
        return await GetPendingRequestsAsync(teamId);
    }

    public async Task ProcessJoinRequestAsync(Guid requestId, bool approve, Guid processedByUserId)
    {
        if (approve)
        {
            await ApproveJoinRequestAsync(requestId);
        }
        else
        {
            await RejectJoinRequestAsync(requestId);
        }
    }

    public async Task ApproveJoinRequestAsync(Guid requestId)
    {
        var request = await _db.GetSingleAsync<JoinRequest>("join_requests", $"id=eq.{requestId}");
        if (request == null) return;
        
        await _db.UpdateAsync<JoinRequest>("join_requests", $"id=eq.{requestId}", new 
        { 
            status = "Approved",
            responded_at = DateTime.UtcNow 
        });

        await AddMemberAsync(request.TeamId, request.UserId);
    }

    public async Task RejectJoinRequestAsync(Guid requestId)
    {
        await _db.UpdateAsync<JoinRequest>("join_requests", $"id=eq.{requestId}", new 
        { 
            status = "Rejected",
            responded_at = DateTime.UtcNow 
        });
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
