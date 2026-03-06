using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Models;
using TechChallenge.Web.Domain;

namespace TechChallenge.Web.Data;

/// <summary>
/// Handles team member management including email uniqueness enforcement.
/// </summary>
public class TeamMemberService(AppDbContext db) : ITeamMemberService
{
    /// <inheritdoc/>
public async Task<PagedResult<TeamMember>> ListAsync(
    bool  onlyActive     = false,
    int   page           = 1,
    int   pageSize       = 15,
    CancellationToken ct = default)
{
    var query = db.TeamMembers
        .AsNoTracking()
        .Include(m => m.AssignedTasks)
        .AsQueryable();

    if (onlyActive)
        query = query.Where(m => m.IsActive);

    query = query.OrderBy(m => m.FullName);

    var totalCount = await query.CountAsync(ct);

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<TeamMember>(items, totalCount, page, pageSize);
}

    /// <inheritdoc/>
    public Task<TeamMember?> GetAsync(int id, CancellationToken ct = default)
        => db.TeamMembers.FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> CreateAsync(TeamMember input, CancellationToken ct = default)
    {
        // Email must be unique across all members
        var duplicate = await db.TeamMembers.AnyAsync(x => x.Email == input.Email, ct);
        if (duplicate)
            return (false, "A member with that email already exists.");

        db.TeamMembers.Add(input);
        LogAsync(input.Id, "MemberCreated", $"Team member '{input.FullName}' ({input.Email}) was added.");
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> UpdateAsync(TeamMember input, CancellationToken ct = default)
    {
        var existing = await db.TeamMembers.FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (existing is null) return (false, "Member not found.");

        // Check email uniqueness excluding current member
        var duplicate = await db.TeamMembers
            .AnyAsync(x => x.Id != input.Id && x.Email == input.Email, ct);

        if (duplicate)
            return (false, "A member with that email already exists.");

            existing.FullName = input.FullName;
            existing.Email    = input.Email;
            LogAsync(existing.Id, "MemberUpdated", $"Team member '{existing.FullName}' was updated.");
            await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.TeamMembers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return false;

        // Toggle active state
            existing.IsActive = !existing.IsActive;
            LogAsync(existing.Id, "MemberStatusChanged", $"'{existing.FullName}' was marked as {(existing.IsActive ? "active" : "inactive")}.");
            await db.SaveChangesAsync(ct);
        return true;
    }
    private void LogAsync(int memberId, string action, string details)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            EntityType    = "TeamMember",
            EntityId      = memberId,
            TaskItemId    = 0,
            Action        = action,
            Actor         = "system",
            Details       = details,
            OccurredAtUtc = DateTime.UtcNow,
        });
    }
    /// <inheritdoc/>
public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
{
    var existing = await db.TeamMembers
        .FirstOrDefaultAsync(x => x.Id == id, ct);

    if (existing is null) return false;

    var tasks = await db.Tasks
        .Where(t => t.AssigneeId == id)
        .ToListAsync(ct);

    Console.WriteLine($"[DeleteAsync] Member {id}, tasks found: {tasks.Count}");

    var taskCount = tasks.Count;
    db.Tasks.RemoveRange(tasks);

    LogAsync(id, "MemberDeleted",
        $"Team member '{existing.FullName}' was deleted along with {taskCount} task(s).");
    db.TeamMembers.Remove(existing);
    await db.SaveChangesAsync(ct);

    return true;
}
}