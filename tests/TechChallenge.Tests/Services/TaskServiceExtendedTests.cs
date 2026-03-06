using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Data;
using TechChallenge.Web.Domain;
using TechChallenge.Web.Models;
using TaskStatus = TechChallenge.Web.Models.TaskStatus;

namespace TechChallenge.Tests.Services;

/// <summary>
/// Extended tests covering fast-close, edit restrictions, and subtask functionality.
/// </summary>
public class TaskServiceExtendedTests : IDisposable
{
    private readonly AppDbContext     _db;
    private readonly TaskStateMachine _stateMachine = new();
    private readonly TaskService      _sut;

    public TaskServiceExtendedTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db  = new AppDbContext(options);
        _sut = new TaskService(_db, _stateMachine);
    }

    // ── Fast-close & CloseReason ───────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_BacklogToDone_RequiresReason()
    {
        var task = await SeedTaskAsync(TaskStatus.Backlog);

        var (ok, err) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.Done, reason: null);

        ok.Should().BeFalse();
        err.Should().Contain("reason");
    }

    [Fact]
    public async Task ChangeStatus_BacklogToDone_WithReason_ShouldMarkFastClosed()
    {
        var task = await SeedTaskAsync(TaskStatus.Backlog);

        var (ok, _) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.Done, reason: "Duplicate");

        ok.Should().BeTrue();

        var updated = await _db.Tasks.FindAsync(task.Id);
        updated!.IsFastClosed.Should().BeTrue();
        updated.CloseReason.Should().Be("Duplicate");
    }

    [Fact]
    public async Task ChangeStatus_BacklogToBlocked_WithReason_ShouldMarkFastClosed()
    {
        var task = await SeedTaskAsync(TaskStatus.Backlog);

        var (ok, _) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.Blocked, reason: "Out of Scope");

        ok.Should().BeTrue();

        var updated = await _db.Tasks.FindAsync(task.Id);
        updated!.IsFastClosed.Should().BeTrue();
        updated.CloseReason.Should().Be("Out of Scope");
    }

    [Fact]
    public async Task ChangeStatus_InProgressToDone_WithReason_ShouldMarkFastClosed()
    {
        var task = await SeedTaskAsync(TaskStatus.InProgress);

        var (ok, _) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.Done, reason: "Won't Fix");

        ok.Should().BeTrue();

        var updated = await _db.Tasks.FindAsync(task.Id);
        updated!.IsFastClosed.Should().BeTrue();
        updated.CloseReason.Should().Be("Won't Fix");
    }

    [Fact]
    public async Task ChangeStatus_Reopen_ShouldClearFastClosedAndReason()
    {
        var task = await SeedTaskAsync(TaskStatus.Backlog);
        await _sut.ChangeStatusAsync(task.Id, TaskStatus.Done, reason: "Duplicate");

        var (ok, _) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.Backlog);

        ok.Should().BeTrue();

        var updated = await _db.Tasks.FindAsync(task.Id);
        updated!.IsFastClosed.Should().BeFalse();
        updated.CloseReason.Should().BeNull();
    }

    // ── Edit restrictions ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_DoneTask_ShouldThrow()
    {
        var task = await SeedTaskAsync(TaskStatus.Done);

        var act = async () => await _sut.UpdateAsync(new TaskItem
        {
            Id        = task.Id,
            Title     = "Modified",
            ProjectId = task.ProjectId,
            Status    = TaskStatus.Done,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Done*");
    }

    [Fact]
    public async Task UpdateAsync_BlockedTask_CanEditTitle()
    {
        var task = await SeedTaskAsync(TaskStatus.Blocked);

        var result = await _sut.UpdateAsync(new TaskItem
        {
            Id            = task.Id,
            Title         = "Updated Title",
            ProjectId     = task.ProjectId,
            AssigneeId    = task.AssigneeId,
            Priority      = task.Priority,
            DueDate       = task.DueDate,
            EstimatedHours = task.EstimatedHours,
            ActualHours   = task.ActualHours,
        });

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_BlockedTask_CannotChangeProject()
    {
        var task    = await SeedTaskAsync(TaskStatus.Blocked);
        var project = await SeedProjectAsync();

        var act = async () => await _sut.UpdateAsync(new TaskItem
        {
            Id            = task.Id,
            Title         = task.Title,
            ProjectId     = project.Id, // different project
            AssigneeId    = task.AssigneeId,
            Priority      = task.Priority,
            DueDate       = task.DueDate,
            EstimatedHours = task.EstimatedHours,
            ActualHours   = task.ActualHours,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*project*");
    }

    [Fact]
    public async Task UpdateAsync_BlockedTask_CannotChangePriority()
    {
        var task = await SeedTaskAsync(TaskStatus.Blocked);

        var act = async () => await _sut.UpdateAsync(new TaskItem
        {
            Id            = task.Id,
            Title         = task.Title,
            ProjectId     = task.ProjectId,
            AssigneeId    = task.AssigneeId,
            Priority      = TaskPriority.Critical, // different priority
            DueDate       = task.DueDate,
            EstimatedHours = task.EstimatedHours,
            ActualHours   = task.ActualHours,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*priority*");
    }

    // ── Subtasks ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSubTaskAsync_ShouldInheritProjectFromParent()
    {
        var parent = await SeedTaskAsync();

        var subTask = await _sut.CreateSubTaskAsync(parent.Id, new TaskItem
        {
            Title    = "Subtask",
            Priority = TaskPriority.Low,
        });

        subTask.ProjectId.Should().Be(parent.ProjectId);
        subTask.ParentTaskId.Should().Be(parent.Id);
        subTask.Status.Should().Be(TaskStatus.Backlog);
    }

    [Fact]
    public async Task CreateSubTaskAsync_InvalidParent_ShouldThrow()
    {
        var act = async () => await _sut.CreateSubTaskAsync(999, new TaskItem
        {
            Title    = "Subtask",
            Priority = TaskPriority.Low,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Parent task not found*");
    }

    [Fact]
    public async Task DeleteSubTaskAsync_ShouldRemoveSubTask()
    {
        var parent  = await SeedTaskAsync();
        var subTask = await _sut.CreateSubTaskAsync(parent.Id, new TaskItem
        {
            Title    = "To delete",
            Priority = TaskPriority.Low,
        });

        await _sut.DeleteSubTaskAsync(subTask.Id);

        var deleted = await _db.Tasks.FindAsync(subTask.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetSubTasksAsync_ShouldReturnOnlySubTasksOfParent()
    {
        var parent1 = await SeedTaskAsync();
        var parent2 = await SeedTaskAsync();

        await _sut.CreateSubTaskAsync(parent1.Id, new TaskItem { Title = "Sub1", Priority = TaskPriority.Low });
        await _sut.CreateSubTaskAsync(parent1.Id, new TaskItem { Title = "Sub2", Priority = TaskPriority.Low });
        await _sut.CreateSubTaskAsync(parent2.Id, new TaskItem { Title = "Sub3", Priority = TaskPriority.Low });

        var subTasks = await _sut.GetSubTasksAsync(parent1.Id);

        subTasks.Should().HaveCount(2);
        subTasks.Should().AllSatisfy(s => s.ParentTaskId.Should().Be(parent1.Id));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<Project> SeedProjectAsync()
    {
        var project = new Project { Name = $"Project {Guid.NewGuid():N}", Code = Guid.NewGuid().ToString("N")[..6].ToUpper() };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    private async Task<TaskItem> SeedTaskAsync(
        TaskStatus status    = TaskStatus.Backlog,
        int?       projectId = null)
    {
        var pid = projectId ?? (await SeedProjectAsync()).Id;

        var task = new TaskItem
        {
            Title     = $"Task {Guid.NewGuid():N}",
            ProjectId = pid,
            Status    = status,
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public void Dispose() => _db.Dispose();
}