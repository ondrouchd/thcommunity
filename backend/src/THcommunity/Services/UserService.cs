using THcommunity.Models;

namespace THcommunity.Services;

public interface IUserService
{
    Task<User?> GetByAuthIdAsync(string authId);
    Task<User?> GetByIdAsync(Guid id);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<IEnumerable<User>> GetTeamMembersAsync(Guid teamId);
    Task<bool> IsAdminOrCoachAsync(Guid userId, Guid teamId);
}

public class UserService : IUserService
{
    private readonly ISupabaseClient _db;
    private readonly ILogger<UserService> _logger;

    public UserService(ISupabaseClient db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<User?> GetByAuthIdAsync(string authId)
    {
        // Quote and escape the authId to ensure proper querying through PostgREST (handles hyphens, special chars)
        var safeAuthId = authId.Replace("'", "''"); // basic SQL-style escaping for single quotes
        var query = $"auth_id=eq.'{safeAuthId}'";
        _logger.LogInformation("Looking up user by auth id. Query={Query}", query);

        var user = await _db.GetSingleAsync<User>("users", query);
        _logger.LogInformation(user == null ? "No user found for auth id {AuthId}" : "Found user {UserId} for auth id {AuthId}", authId, user?.Id);
        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.GetSingleAsync<User>("users", $"id=eq.{id}");
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        return await _db.InsertAsync("users", user);
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        
        return await _db.UpdateAsync<User>("users", $"id=eq.{user.Id}", user);
    }

    public async Task<IEnumerable<User>> GetTeamMembersAsync(Guid teamId)
    {
        return await _db.GetListAsync<User>("users", $"team_id=eq.{teamId}");
    }

    public async Task<bool> IsAdminOrCoachAsync(Guid userId, Guid teamId)
    {
        var user = await GetByIdAsync(userId);
        
        if (user == null || user.TeamId != teamId)
            return false;
        
        return user.Role == UserRole.Admin || user.Role == UserRole.Coach;
    }
}
