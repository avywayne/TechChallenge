using TechChallenge.Web.Models;
using TaskStatus = TechChallenge.Web.Models.TaskStatus;
using TechChallenge.Web.Domain;

namespace TechChallenge.Web.Data;

public interface ITaskService
{
/// <summary>Returns a paginated list of tasks with optional filters.</summary>
Task<PagedResult<TaskItem>> ListAsync(
    int?          projectId  = null,
    TaskStatus?   status     = null,
    TaskPriority? priority   = null,
    int?          assigneeId = null,
    string?       search     = null,
    string?       sortBy     = null,
    int           page       = 1,
    int           pageSize   = 15,
    CancellationToken ct     = default);

    Task<TaskItem?> GetAsync(int id, CancellationToken ct = default);
    Task<TaskItem> CreateAsync(TaskItem input, CancellationToken ct = default);
    Task<TaskItem?> UpdateAsync(TaskItem input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<(bool Success, string? Error)> ChangeStatusAsync(
        int        taskId,
        TaskStatus newStatus,
        string?    reason = null,
        CancellationToken ct = default);
        
    /// <summary>Returns all subtasks for a given parent task.</summary>
    Task<List<TaskItem>> GetSubTasksAsync(int parentTaskId, CancellationToken ct = default);

    /// <summary>Creates a subtask under a parent task.</summary>
    Task<TaskItem> CreateSubTaskAsync(int parentTaskId, TaskItem subTask, CancellationToken ct = default);

    /// <summary>Deletes a subtask.</summary>
    Task DeleteSubTaskAsync(int subTaskId, CancellationToken ct = default);
}