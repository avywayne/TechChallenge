# Architecture & Design Notes

This document explains the key design decisions made during the implementation of TaskFlow Pro.

---

## 1. State Machine Design

The task lifecycle is enforced by `TaskStateMachine`, a dedicated domain class that owns all transition rules. Each transition is defined as a record with metadata:

```csharp
record StateTransition(
    TaskStatus From,
    TaskStatus To,
    string Label,
    string Description,
    bool RequiresReason,
    bool IsFastClose
);
```

**Why a dedicated state machine class?**
Encoding transitions in a single place makes the rules explicit and testable. The service layer calls `stateMachine.Transition(from, to)` and receives a typed result — it never contains conditional logic about what transitions are valid.

**Fast-close tracking:**
Transitions that skip the normal `InProgress` step (Backlog → Done, Backlog → Blocked, InProgress → Done) are flagged as `IsFastClose = true`. When executed, the task is marked with `IsFastClosed = true` and stores the `CloseReason`. This allows the dashboard and reports to surface tasks that bypassed the expected workflow.

---

## 2. Optimistic Concurrency

Task edits use PostgreSQL's built-in `xmin` system column as a row version token:

```csharp
entity.Property(x => x.RowVersion)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .IsRowVersion()
    .ValueGeneratedOnAddOrUpdate();
```

`xmin` is automatically incremented by PostgreSQL on every row modification. If two users edit the same task simultaneously, the second `SaveChanges` throws a `DbUpdateConcurrencyException`, which the service catches and rethrows as a user-friendly `InvalidOperationException`. This avoids silent data overwrites without requiring a dedicated version column.

---

## 3. Optimistic UI

State transitions and edits update the in-memory `_tasks` list immediately before awaiting the server response. If the server returns an error, the change is reverted and a toast notification is shown. This pattern eliminates the perceived latency of Blazor Server's SignalR round-trips.

`ShowToastAsync` was also changed from blocking (`await Task.Delay(3000)`) to fire-and-forget using `Task.Delay(...).ContinueWith(...)`. The original implementation blocked the Blazor render loop for 3 seconds after every operation.

---

## 4. Activity Log Design

The `ActivityLog` table is polymorphic — it tracks events for tasks, projects, and team members using `EntityType` (string) and `EntityId` (int) columns alongside the original `TaskItemId` foreign key.

Each service owns its own `LogAsync` method (synchronous, no extra `SaveChangesAsync`) that adds the log entry to the same EF change tracker as the main operation. Both the entity change and the log are committed in a single `SaveChangesAsync` call, ensuring they are always consistent.

Task logs include a `['Task Title']` prefix in the `Details` field. This allows the Activity Log page to display the task name even after the task has been deleted, by extracting it from the stored details string.

---

## 5. Edit Restrictions by Status

`TaskService.UpdateAsync` enforces field-level edit restrictions:

- **Done tasks** cannot be edited at all — any update throws `InvalidOperationException`
- **Blocked tasks** can only have their `Title` and `Description` changed — changes to project, assignee, priority, due date, or hours are rejected

The UI reflects these restrictions by disabling the relevant form fields and showing an alert banner inside the edit modal.

---

## 6. Cascade Delete Strategy

When a project or team member is deleted, their associated tasks are also deleted:

- **Project → Tasks**: configured via `OnDelete(DeleteBehavior.Cascade)` in `AppDbContext.OnModelCreating`, handled at the database level.
- **TeamMember → Tasks**: because `AssigneeId` is nullable, PostgreSQL would set it to `NULL` rather than cascade. Tasks are therefore deleted explicitly in `TeamMemberService.DeleteAsync` before removing the member.

---

## 7. Dashboard Query Optimization

The dashboard previously executed 6+ independent `CountAsync` queries sequentially. These were consolidated by defining a reusable base `IQueryable` filter:

```csharp
var baseTasks = Db.Tasks.Where(x =>
    x.ParentTaskId == null &&
    !x.Project!.IsArchived &&
    (x.AssigneeId == null || x.Assignee!.IsActive));
```

All KPI counts, the status distribution dictionary, and the fast-closed table reuse this filter, reducing code duplication and ensuring consistent filtering across all dashboard metrics.

Subtasks (`ParentTaskId != null`) are excluded from all dashboard KPIs to avoid double-counting.

---

## 8. Known Limitations & Technical Debt

- **Sidebar on mobile**: The responsive sidebar overlay does not reliably close on click outside on all devices due to Blazor Server's event handling over SignalR. A pure JavaScript implementation was attempted but not completed. The swipe-to-close gesture works correctly.

- **No authentication**: The application has no user authentication. The `Actor` field in `ActivityLog` is hardcoded to `"system"`. In a production system, this would be populated from the authenticated user's identity.

- **Single DbContext per request**: The dashboard cannot parallelize queries with `Task.WhenAll` because EF Core's `DbContext` is not thread-safe. Queries must run sequentially. A production system could use multiple DbContext instances or raw SQL for dashboard aggregations.

- **In-memory filter options**: The dynamic filter dropdowns on the Tasks page were partially implemented but reverted due to state synchronization issues between the paged result set and the full dataset. Currently all filter options always show all available values.
