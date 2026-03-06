namespace TechChallenge.Web.Models;

public enum TaskPriority { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum TaskStatus { Backlog = 1, InProgress = 2, Blocked = 3, Done = 4 }

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.Backlog;
    public DateTime? DueDate { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? AssigneeId { get; set; }
    public TeamMember? Assignee { get; set; }

    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Row version token used for optimistic concurrency control.
    /// EF Core automatically increments this on every update.
    /// If two users edit the same task simultaneously, the second save will throw
    /// a DbUpdateConcurrencyException instead of silently overwriting changes.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// If set, this task is a subtask of the referenced parent.
    /// Null means this is a top-level task.
    /// </summary>
    public int? ParentTaskId { get; set; }

    // Navigation properties
    public TaskItem?       Parent    { get; set; }
    public List<TaskItem>  SubTasks  { get; set; } = new();

    /// <summary>
    /// Reason provided when a task is closed or blocked without following the normal flow.
    /// </summary>
    public string? CloseReason { get; set; }

    /// <summary>
    /// True when the task reached Done or Blocked directly from Backlog,
    /// skipping the normal InProgress step.
    /// </summary>
    public bool IsFastClosed { get; set; }
}
