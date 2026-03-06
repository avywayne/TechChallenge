using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Models;
using TechChallenge.Web.Domain;

namespace TechChallenge.Web.Data;

/// <summary>
/// Handles project management including archive/unarchive and uniqueness validation.
/// </summary>
public class ProjectService(AppDbContext db) : IProjectService
{
    /// <inheritdoc/>
    public async Task<PagedResult<Project>> ListAsync(
        bool  includeArchived = false,
        int   page            = 1,
        int   pageSize        = 15,
        CancellationToken ct  = default)
    {
        var query = db.Projects
            .AsNoTracking()
            .Include(p => p.Tasks)
            .AsQueryable();

        if (!includeArchived)
            query = query.Where(p => !p.IsArchived);

        query = query.OrderBy(p => p.Name);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Project>(items, totalCount, page, pageSize);
    }

    /// <inheritdoc/>
    public Task<Project?> GetAsync(int id, CancellationToken ct = default)
        => db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> CreateAsync(Project input, CancellationToken ct = default)
    {
        // Enforce uniqueness at service layer in addition to DB constraint
        var duplicate = await db.Projects
            .AnyAsync(x => x.Name == input.Name || x.Code == input.Code, ct);

        if (duplicate)
            return (false, "A project with that name or code already exists.");

        db.Projects.Add(input);
        LogAsync(input.Id, "ProjectCreated", $"Project '{input.Name}' ({input.Code}) was created.");
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> UpdateAsync(Project input, CancellationToken ct = default)
    {
        var existing = await db.Projects.FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (existing is null) return (false, "Project not found.");

        // Check uniqueness excluding the current project
        var duplicate = await db.Projects
            .AnyAsync(x => x.Id != input.Id && (x.Name == input.Name || x.Code == input.Code), ct);

        if (duplicate)
            return (false, "A project with that name or code already exists.");

            existing.Name = input.Name;
            existing.Code = input.Code;
            LogAsync(existing.Id, "ProjectUpdated", $"Project '{existing.Name}' was updated.");
            await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<bool> ArchiveAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return false;

        // Toggle archive state
            existing.IsArchived = !existing.IsArchived;
            LogAsync(existing.Id, "ProjectArchived", $"Project '{existing.Name}' was {(existing.IsArchived ? "archived" : "unarchived")}.");
            await db.SaveChangesAsync(ct);
        return true;
    }

    private void LogAsync(int projectId, string action, string details)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            EntityType    = "Project",
            EntityId      = projectId,
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
        var existing = await db.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (existing is null) return false;

        var taskCount = existing.Tasks.Count;
            LogAsync(id, "ProjectDeleted", $"Project '{existing.Name}' was deleted along with {taskCount} task(s).");
            db.Projects.Remove(existing);
            await db.SaveChangesAsync(ct);
        return true;
    }
}