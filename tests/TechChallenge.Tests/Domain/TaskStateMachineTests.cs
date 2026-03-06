using FluentAssertions;
using TechChallenge.Web.Domain;
using TaskStatus = TechChallenge.Web.Models.TaskStatus;

namespace TechChallenge.Tests.Domain;

/// <summary>
/// Tests for TaskStateMachine — verifies all valid and invalid transitions.
/// These tests serve as living documentation of the task lifecycle rules.
/// </summary>
public class TaskStateMachineTests
{
    private readonly TaskStateMachine _sut = new();

    // ── Valid transitions ──────────────────────────────────────────────────

    [Fact]
    public void Backlog_To_InProgress_ShouldSucceed()
    {
        var result = _sut.Transition(TaskStatus.Backlog, TaskStatus.InProgress);

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void InProgress_To_Blocked_ShouldSucceed()
    {
        var result = _sut.Transition(TaskStatus.InProgress, TaskStatus.Blocked);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void InProgress_To_Done_ShouldSucceed()
    {
        var result = _sut.Transition(TaskStatus.InProgress, TaskStatus.Done);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Blocked_To_InProgress_ShouldFail()
    {
        var result = _sut.Transition(TaskStatus.Blocked, TaskStatus.InProgress);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Blocked_To_Done_ShouldSucceed()
    {
        var result = _sut.Transition(TaskStatus.Blocked, TaskStatus.Done);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Done_To_Backlog_ShouldSucceed()
    {
        var result = _sut.Transition(TaskStatus.Done, TaskStatus.Backlog);

        result.Success.Should().BeTrue();
    }

    // ── Invalid transitions ────────────────────────────────────────────────

    [Fact]
    public void Backlog_To_Done_RequiresReason()
    {
        var result = _sut.Transition(TaskStatus.Backlog, TaskStatus.Done);
        result.Success.Should().BeTrue();
        result.Transition!.RequiresReason.Should().BeTrue();
        result.Transition.IsFastClose.Should().BeTrue();
    }

    [Fact]
    public void Backlog_To_Blocked_RequiresReason()
    {
        var result = _sut.Transition(TaskStatus.Backlog, TaskStatus.Blocked);
        result.Success.Should().BeTrue();
        result.Transition!.RequiresReason.Should().BeTrue();
        result.Transition.IsFastClose.Should().BeTrue();
    }

    [Fact]
    public void Done_To_InProgress_ShouldFail()
    {
        var result = _sut.Transition(TaskStatus.Done, TaskStatus.InProgress);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Done_To_Blocked_ShouldFail()
    {
        var result = _sut.Transition(TaskStatus.Done, TaskStatus.Blocked);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void SameStatus_ShouldFail()
    {
        var result = _sut.Transition(TaskStatus.InProgress, TaskStatus.InProgress);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already");
    }

    // ── Theory: all valid transitions ──────────────────────────────────────

    [Theory]
    [InlineData(TaskStatus.Backlog,    TaskStatus.InProgress)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Blocked)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Done)]
    [InlineData(TaskStatus.Blocked,    TaskStatus.Done)]
    [InlineData(TaskStatus.Done,       TaskStatus.Backlog)]
    public void ValidTransitions_ShouldAlwaysSucceed(TaskStatus from, TaskStatus to)
    {
        var result = _sut.Transition(from, to);

        result.Success.Should().BeTrue(
            $"transition from {from} to {to} should be valid");
    }

    [Theory]
    [InlineData(TaskStatus.Done,       TaskStatus.InProgress)]
    [InlineData(TaskStatus.Done,       TaskStatus.Blocked)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Backlog)]
    [InlineData(TaskStatus.Blocked,    TaskStatus.Backlog)]
    public void InvalidTransitions_ShouldAlwaysFail(TaskStatus from, TaskStatus to)
    {
        var result = _sut.Transition(from, to);

        result.Success.Should().BeFalse(
            $"transition from {from} to {to} should not be allowed");
    }

    // ── GetAvailableTransitions ────────────────────────────────────────────

    [Fact]
    public void Backlog_ShouldHaveThreeTransitions()
    {
        var transitions = _sut.GetAvailableTransitions(TaskStatus.Backlog);
        transitions.Should().HaveCount(3);
        transitions.Should().Contain(t => t.To == TaskStatus.InProgress && !t.RequiresReason);
        transitions.Should().Contain(t => t.To == TaskStatus.Blocked    && t.RequiresReason);
        transitions.Should().Contain(t => t.To == TaskStatus.Done       && t.RequiresReason);
    }

    [Fact]
    public void InProgress_ShouldHaveTwoTransitions()
    {
        var transitions = _sut.GetAvailableTransitions(TaskStatus.InProgress);

        transitions.Should().HaveCount(2);
        transitions.Select(t => t.To).Should().Contain(TaskStatus.Blocked);
        transitions.Select(t => t.To).Should().Contain(TaskStatus.Done);
    }

    [Fact]
    public void Done_ShouldHaveReopenTransition()
    {
        var transitions = _sut.GetAvailableTransitions(TaskStatus.Done);

        transitions.Should().HaveCount(1);
        transitions.Single().To.Should().Be(TaskStatus.Backlog);
        transitions.Single().Label.Should().Be("Reopen");
    }

    [Fact]
    public void AllTransitions_ShouldHaveLabelsAndDescriptions()
    {
        var all = _sut.GetAllTransitions();

        all.Should().AllSatisfy(t =>
        {
            t.Label.Should().NotBeNullOrWhiteSpace();
            t.Description.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── CanTransition ──────────────────────────────────────────────────────

    [Fact]
    public void CanTransition_ValidPair_ReturnsTrue()
    {
        _sut.CanTransition(TaskStatus.Backlog, TaskStatus.InProgress).Should().BeTrue();
    }

    [Fact]
    public void CanTransition_InvalidPair_ReturnsFalse()
    {
        _sut.CanTransition(TaskStatus.Done, TaskStatus.InProgress).Should().BeFalse();
    }
}