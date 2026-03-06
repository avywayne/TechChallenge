using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Domain;
using TechChallenge.Web.Models;
using TaskStatus = TechChallenge.Web.Models.TaskStatus;


namespace TechChallenge.Web.Data;

/// <summary>
/// Handles all task-related business logic including status transitions and audit logging.
/// Status transition rules are delegated to <see cref="TaskStateMachine"/>.
/// </summary>
public class TaskService(AppDbContext db, TaskStateMachine stateMachine) : ITaskService
{
    /// <summary>
    /// Defines which status transitions are valid.
    /// Done -> Backlog represents a "Reopen" action.
    /// </summary>
    private static readonly Dictionary<TaskStatus, TaskStatus[]> AllowedTransitions = new()
    {
        [TaskStatus.Backlog]    = [TaskStatus.InProgress],
        [TaskStatus.InProgress] = [TaskStatus.Blocked, TaskStatus.Done],
        [TaskStatus.Blocked]    = [TaskStatus.InProgress, TaskStatus.Done],
        [TaskStatus.Done]       = [TaskStatus.Backlog], // Reopen
    };

    /// <inheritdoc/>
  /// <inheritdoc/>
public async Task<PagedResult<TaskItem>> ListAsync(
    int?          projectId  = null,
    TaskStatus?   status     = null,
    TaskPriority? priority   = null,
    int?          assigneeId = null,
    string?       search     = null,
    string?       sortBy     = null,
    int           page       = 1,
    int           pageSize   = 15,
    CancellationToken ct     = default
    )
{
    var query = db.Tasks
        .AsNoTracking()
        .Include(t => t.Project)
        .Include(t => t.Assignee)
        .Include(t => t.SubTasks)
        .AsQueryable();
        // Only return top-level tasks — subtasks are loaded separately per parent
        query = query.Where(t => t.ParentTaskId == null);

    // Apply filters
    if (projectId.HasValue)
        query = query.Where(t => t.ProjectId == projectId.Value);

    if (status.HasValue)
        query = query.Where(t => t.Status == status.Value);

    if (priority.HasValue)
        query = query.Where(t => t.Priority == priority.Value);

    if (assigneeId.HasValue)
        query = query.Where(t => t.AssigneeId == assigneeId.Value);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim().ToLower();
        query = query.Where(t =>
            t.Title.ToLower().Contains(term) ||
            (t.Description != null && t.Description.ToLower().Contains(term)));
    }

    // Apply sort
    query = sortBy switch
    {
        "dueDate"     => query.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate),
        "updatedDate" => query.OrderByDescending(t => t.UpdatedAtUtc),
        _             => query.OrderByDescending(t => t.Priority)
    };

    // Get total before pagination
    var totalCount = await query.CountAsync(ct);

    // Apply pagination
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<TaskItem>(items, totalCount, page, pageSize);
}

    /// <inheritdoc/>
    public Task<TaskItem?> GetAsync(int id, CancellationToken ct = default)
        => db.Tasks
            .Include(x => x.Project)
            .Include(x => x.Assignee)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <inheritdoc/>
    public async Task<TaskItem> CreateAsync(TaskItem input, CancellationToken ct = default)
    {
        // Ensure timestamps are set server-side
        input.CreatedAtUtc = DateTime.UtcNow;
        input.UpdatedAtUtc = DateTime.UtcNow;

        db.Tasks.Add(input);
        LogAsync(input.Id, "TaskCreated", $"Task '{input.Title}' was created.", input.Title);
        await db.SaveChangesAsync(ct);

        // Audit: log task creation

        return input;
    }

    /// <inheritdoc/>
    public async Task<TaskItem?> UpdateAsync(TaskItem updated, CancellationToken ct = default)
    {
        var entity = await db.Tasks.FirstOrDefaultAsync(x => x.Id == updated.Id, ct);
        if (entity is null) return null;

        // Done tasks cannot be edited at all
        if (entity.Status == TaskStatus.Done)
            throw new InvalidOperationException("Done tasks cannot be edited.");

        // Blocked tasks have restricted fields
        if (entity.Status == TaskStatus.Blocked)
        {
            if (updated.ProjectId      != entity.ProjectId)      throw new InvalidOperationException("Cannot change the project of a blocked task.");
            if (updated.AssigneeId     != entity.AssigneeId)     throw new InvalidOperationException("Cannot change the assignee of a blocked task.");
            if (updated.Priority       != entity.Priority)       throw new InvalidOperationException("Cannot change the priority of a blocked task.");
            if (updated.DueDate        != entity.DueDate)        throw new InvalidOperationException("Cannot change the due date of a blocked task.");
            if (updated.EstimatedHours != entity.EstimatedHours) throw new InvalidOperationException("Cannot change estimated hours of a blocked task.");
            if (updated.ActualHours    != entity.ActualHours)    throw new InvalidOperationException("Cannot change actual hours of a blocked task.");
        }

        var oldAssigneeId = entity.AssigneeId;

        entity.Title          = updated.Title;
        entity.Description    = updated.Description;
        entity.Priority       = updated.Priority;
        entity.ProjectId      = updated.ProjectId;
        entity.AssigneeId     = updated.AssigneeId;
        entity.DueDate        = updated.DueDate;
        entity.EstimatedHours = updated.EstimatedHours;
        entity.ActualHours    = updated.ActualHours;
        entity.UpdatedAtUtc   = DateTime.UtcNow;

        // Log assignee change before saving
        if (oldAssigneeId != updated.AssigneeId)
            LogAsync(entity.Id, "AssigneeChanged",
                $"Assignee changed from id:{oldAssigneeId} to id:{updated.AssigneeId}.", entity.Title);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await ex.Entries.Single().ReloadAsync(ct);
            throw new InvalidOperationException(
                "This task was modified by another user while you were editing it. " +
                "Please review the latest changes and try again.", ex);
        }

        return entity;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        // Audit before deletion so we retain the task title in the log
        LogAsync(entity.Id, "TaskDeleted", $"Task '{entity.Title}' was deleted.", entity.Title);
        db.Tasks.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc/>
    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> ChangeStatusAsync(
        int        taskId,
        TaskStatus newStatus,
        string?    reason     = null,
        CancellationToken ct  = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (task is null) return (false, "Task not found.");

        var result = stateMachine.Transition(task.Status, newStatus);
        if (!result.Success) return (false, result.Error);

        // Validate reason when required
        if (result.Transition!.RequiresReason && string.IsNullOrWhiteSpace(reason))
            return (false, "A reason is required for this transition.");

        var oldStatus = task.Status;
        task.Status = newStatus;

        // Apply close reason and fast-close mark
        if (!string.IsNullOrWhiteSpace(reason))
            task.CloseReason = reason.Trim();

        if (result.Transition.IsFastClose)
            task.IsFastClosed = true;

        // Clear fast-close mark when task is reopened
        if (newStatus == TaskStatus.Backlog)
        {
            task.IsFastClosed = false;
            task.CloseReason  = null;
        }
        task.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(reason)) task.CloseReason = reason.Trim();
        if (result.Transition.IsFastClose) task.IsFastClosed = true;
        if (newStatus == TaskStatus.Backlog) { task.IsFastClosed = false; task.CloseReason = null; }

        LogAsync(task.Id, "StatusChanged",
            $"Status changed from {oldStatus} to {newStatus}" +
            (string.IsNullOrEmpty(reason) ? "." : $". Reason: {reason}"), task.Title);

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// Writes an audit entry to the ActivityLogs table.
    /// Actor defaults to "system" until authentication is implemented.
    /// </summary>
  private void LogAsync(int taskId, string action, string details, string? title = null)
    {
        var fullDetails = title is not null
            ? $"['{title}'] {details}"
            : details;

        db.ActivityLogs.Add(new ActivityLog
        {
            EntityType    = "Task",
            EntityId      = taskId,
            TaskItemId    = taskId,
            Action        = action,
            Actor         = "system",
            Details       = fullDetails,
            OccurredAtUtc = DateTime.UtcNow,
        });
        // No SaveChangesAsync here — caller is responsible
    }
    /// <inheritdoc/>
public async Task<List<TaskItem>> GetSubTasksAsync(
    int parentTaskId, CancellationToken ct = default)
{
    return await db.Tasks
        .AsNoTracking()
        .Include(t => t.Assignee)
        .Where(t => t.ParentTaskId == parentTaskId)
        .OrderBy(t => t.CreatedAtUtc)
        .ToListAsync(ct);
}

/// <inheritdoc/>
    public async Task<TaskItem> CreateSubTaskAsync(
        int parentTaskId, TaskItem subTask, CancellationToken ct = default)
    {
        var parent = await db.Tasks.FirstOrDefaultAsync(t => t.Id == parentTaskId, ct);
        if (parent is null) throw new InvalidOperationException("Parent task not found.");

        subTask.ParentTaskId  = parentTaskId;
        subTask.ProjectId     = parent.ProjectId;
        subTask.Status        = TaskStatus.Backlog;
        subTask.CreatedAtUtc  = DateTime.UtcNow;
        subTask.UpdatedAtUtc  = DateTime.UtcNow;

        db.Tasks.Add(subTask);
        LogAsync(subTask.Id, "TaskCreated",
            $"Subtask '{subTask.Title}' created under task id:{parentTaskId}.", subTask.Title);
        await db.SaveChangesAsync(ct);

        return subTask;
    }

    /// <inheritdoc/>
    public async Task DeleteSubTaskAsync(int subTaskId, CancellationToken ct = default)
    {
        var entity = await db.Tasks.FirstOrDefaultAsync(t => t.Id == subTaskId, ct);
        if (entity is null) return;

        LogAsync(subTaskId, "TaskDeleted", $"Subtask '{entity.Title}' deleted.", entity.Title);
        db.Tasks.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}