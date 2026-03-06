using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Data;
using TechChallenge.Web.Domain;
using TechChallenge.Web.Models;
using TaskStatus = TechChallenge.Web.Models.TaskStatus;

namespace TechChallenge.Tests.Services;

/// <summary>
/// Integration-style tests for TaskService using an in-memory database.
/// Tests cover business rules, audit logging, and status transition enforcement.
/// </summary>
public class TaskServiceTests : IDisposable
{
    private readonly AppDbContext    _db;
    private readonly TaskStateMachine _stateMachine = new();
    private readonly TaskService     _sut;

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Isolated DB per test
            .Options;

        _db  = new AppDbContext(options);
        _sut = new TaskService(_db, _stateMachine);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldPersistTask()
    {
        var project = await SeedProjectAsync();

        var task = new TaskItem
        {
            Title     = "New task",
            ProjectId = project.Id,
            Priority  = TaskPriority.Medium,
        };

        var created = await _sut.CreateAsync(task);

        created.Id.Should().BeGreaterThan(0);
        created.Status.Should().Be(TaskStatus.Backlog);
        created.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldDefaultToBacklog()
    {
        var project = await SeedProjectAsync();
        var task    = new TaskItem { Title = "Test", ProjectId = project.Id };

        var created = await _sut.CreateAsync(task);

        created.Status.Should().Be(TaskStatus.Backlog);
    }

    [Fact]
    public async Task CreateAsync_ShouldWriteActivityLog()
    {
        var project = await SeedProjectAsync();
        var task    = new TaskItem { Title = "Audit test", ProjectId = project.Id };

        var created = await _sut.CreateAsync(task);

        var log = await _db.ActivityLogs
            .FirstOrDefaultAsync(l => l.TaskItemId == created.Id && l.Action == "TaskCreated");

        log.Should().NotBeNull();
        log!.Details.Should().Contain("Audit test");
    }

    // ── ChangeStatusAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_ValidTransition_ShouldSucceed()
    {
        var task = await SeedTaskAsync(TaskStatus.Backlog);

        var (ok, err) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.InProgress);

        ok.Should().BeTrue();
        err.Should().BeNull();

        var updated = await _db.Tasks.FindAsync(task.Id);
        updated!.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_ShouldFail()
    {
        var task = await SeedTaskAsync(TaskStatus.Backlog);

        var (ok, err) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.Done);

        ok.Should().BeFalse();
        err.Should().NotBeNullOrEmpty();

        // Status should remain unchanged
        var unchanged = await _db.Tasks.FindAsync(task.Id);
        unchanged!.Status.Should().Be(TaskStatus.Backlog);
    }

    [Fact]
    public async Task ChangeStatus_ShouldWriteActivityLog()
    {
        var task = await SeedTaskAsync(TaskStatus.Backlog);

        await _sut.ChangeStatusAsync(task.Id, TaskStatus.InProgress);

        var log = await _db.ActivityLogs
            .FirstOrDefaultAsync(l =>
                l.TaskItemId == task.Id &&
                l.Action     == "StatusChanged");

        log.Should().NotBeNull();
        log!.Details.Should().Contain("Backlog");
        log.Details.Should().Contain("InProgress");
    }

    [Fact]
    public async Task ChangeStatus_NonExistentTask_ShouldFail()
    {
        var (ok, err) = await _sut.ChangeStatusAsync(999, TaskStatus.InProgress);

        ok.Should().BeFalse();
        err.Should().Contain("not found");
    }

    [Fact]
    public async Task ChangeStatus_SameStatus_ShouldFail()
    {
        var task = await SeedTaskAsync(TaskStatus.InProgress);

        var (ok, err) = await _sut.ChangeStatusAsync(task.Id, TaskStatus.InProgress);

        ok.Should().BeFalse();
        err.Should().Contain("already");
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask()
    {
        var task = await SeedTaskAsync();

        await _sut.DeleteAsync(task.Id);

        var deleted = await _db.Tasks.FindAsync(task.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldWriteActivityLog()
    {
        var task = await SeedTaskAsync();

        await _sut.DeleteAsync(task.Id);

        var log = await _db.ActivityLogs
            .FirstOrDefaultAsync(l =>
                l.TaskItemId == task.Id &&
                l.Action     == "TaskDeleted");

        log.Should().NotBeNull();
    }

    // ── ListAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_WithStatusFilter_ShouldReturnOnlyMatchingTasks()
    {
        var project = await SeedProjectAsync();
        await SeedTaskAsync(TaskStatus.Backlog,    project.Id);
        await SeedTaskAsync(TaskStatus.InProgress, project.Id);
        await SeedTaskAsync(TaskStatus.Done,       project.Id);

        var result = await _sut.ListAsync(status: TaskStatus.Backlog);

        result.Items.Should().AllSatisfy(t => t.Status.Should().Be(TaskStatus.Backlog));
    }

    [Fact]
    public async Task ListAsync_WithSearch_ShouldReturnMatchingTasks()
    {
        var project = await SeedProjectAsync();

        await _sut.CreateAsync(new TaskItem
        {
            Title     = "Fix authentication bug",
            ProjectId = project.Id
        });
        await _sut.CreateAsync(new TaskItem
        {
            Title     = "Unrelated task",
            ProjectId = project.Id
        });

        var result = await _sut.ListAsync(search: "authentication");

        result.Items.Should().HaveCount(1);
        result.Items.Single().Title.Should().Contain("authentication");
    }

    [Fact]
    public async Task ListAsync_Pagination_ShouldReturnCorrectPage()
    {
        var project = await SeedProjectAsync();

        // Create 10 tasks
        for (var i = 1; i <= 10; i++)
            await _sut.CreateAsync(new TaskItem { Title = $"Task {i}", ProjectId = project.Id });

        var page1 = await _sut.ListAsync(page: 1, pageSize: 3);
        var page2 = await _sut.ListAsync(page: 2, pageSize: 3);

        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(3);
        page1.TotalCount.Should().Be(10);
        page1.TotalPages.Should().Be(4);
        page1.HasNext.Should().BeTrue();
        page1.HasPrevious.Should().BeFalse();
        page2.HasPrevious.Should().BeTrue();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<Project> SeedProjectAsync()
    {
        var project = new Project { Name = "Test Project", Code = "TST" };
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
            Title = $"Task {Guid.NewGuid():N}",
            ProjectId = pid,
            Status    = status,
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public void Dispose() => _db.Dispose();
}