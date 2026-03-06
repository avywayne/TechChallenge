namespace TechChallenge.Web.Models;

/// <summary>
/// Audit log entry for any entity lifecycle event.
/// Supports Tasks, Projects, and Team Members via EntityType + EntityId.
/// TaskItemId is kept for backwards compatibility with existing task logs.
/// </summary>
public class ActivityLog
{
    public int     Id            { get; set; }

    /// <summary>Type of entity that triggered the event: "Task", "Project", "Member".</summary>
    public string  EntityType    { get; set; } = "Task";

    /// <summary>ID of the affected entity.</summary>
    public int     EntityId      { get; set; }

    /// <summary>Kept for backwards compatibility — same as EntityId when EntityType is "Task".</summary>
    public int     TaskItemId    { get; set; }

    public string  Action        { get; set; } = "";
    public string  Actor         { get; set; } = "system";
    public string? Details       { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
