using TechChallenge.Web.Models;
using TaskStatus = TechChallenge.Web.Models.TaskStatus;

namespace TechChallenge.Web.Domain;

/// <summary>
/// Defines all valid task status transitions and whether a reason is required.
/// Single source of truth for both UI and service layer.
/// </summary>
public class TaskStateMachine
{
    /// <summary>
    /// All valid transitions with metadata.
    /// RequiresReason = true means the user must provide a justification.
    /// IsFastClose = true means the task skipped the normal InProgress flow.
    /// </summary>
    private static readonly IReadOnlyList<StateTransition> Transitions = new[]
    {
        new StateTransition(TaskStatus.Backlog,    TaskStatus.InProgress, "Start",        "Begin working on the task",             RequiresReason: false, IsFastClose: false),
        new StateTransition(TaskStatus.Backlog,    TaskStatus.Blocked,    "Block",         "Mark as blocked without starting",      RequiresReason: true,  IsFastClose: true),
        new StateTransition(TaskStatus.Backlog,    TaskStatus.Done,       "Close",         "Close without working on it",           RequiresReason: true,  IsFastClose: true),
        new StateTransition(TaskStatus.InProgress, TaskStatus.Blocked,    "Block",         "Something is preventing progress",      RequiresReason: false, IsFastClose: false),
        new StateTransition(TaskStatus.InProgress, TaskStatus.Done,       "Complete",      "Mark as done",                          RequiresReason: true,  IsFastClose: true),
        new StateTransition(TaskStatus.Blocked,    TaskStatus.Done,       "Close",         "Close while still blocked",             RequiresReason: false, IsFastClose: false),
        new StateTransition(TaskStatus.Done,       TaskStatus.Backlog,    "Reopen",        "Reopen for rework",                     RequiresReason: false, IsFastClose: false),
    };

    public TransitionResult Transition(TaskStatus from, TaskStatus to)
    {
        if (from == to)
            return TransitionResult.Fail("Task is already in that status.");

        var t = Transitions.FirstOrDefault(x => x.From == from && x.To == to);
        if (t is null)
            return TransitionResult.Fail($"Cannot transition from {from} to {to}.");

        return TransitionResult.Ok(t);
    }

    public IReadOnlyList<StateTransition> GetAvailableTransitions(TaskStatus current)
        => Transitions.Where(t => t.From == current).ToList();

    public bool RequiresReason(TaskStatus from, TaskStatus to)
    {
        var t = Transitions.FirstOrDefault(x => x.From == from && x.To == to);
        return t?.RequiresReason ?? false;
    }

    public bool IsFastClose(TaskStatus from, TaskStatus to)
    {
        var t = Transitions.FirstOrDefault(x => x.From == from && x.To == to);
        return t?.IsFastClose ?? false;
    }

    public bool CanTransition(TaskStatus from, TaskStatus to)
        => Transitions.Any(x => x.From == from && x.To == to);

    public IReadOnlyList<StateTransition> GetAllTransitions() => Transitions;

}

public record TransitionResult(bool Success, string? Error, StateTransition? Transition)
{
    public static TransitionResult Ok(StateTransition t)   => new(true,  null,  t);
    public static TransitionResult Fail(string error)       => new(false, error, null);
}

public record StateTransition(
    TaskStatus From,
    TaskStatus To,
    string     Label,
    string     Description,
    bool       RequiresReason,
    bool       IsFastClose);
