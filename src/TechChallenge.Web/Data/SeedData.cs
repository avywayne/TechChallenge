using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Models;
using TaskStatus = TechChallenge.Web.Models.TaskStatus;

namespace TechChallenge.Web.Data;

/// <summary>
/// Professional seed data for reviewer demonstration.
/// Simulates a realistic software team with 3 active projects,
/// multiple team members, varied task states, and a rich audit trail.
///
/// Run via: dotnet run --project src/TechChallenge.Web -- --seed
/// Reset:   psql -U postgres -d taskflowpro -c "TRUNCATE \"ActivityLogs\", \"Tasks\", \"TeamMembers\", \"Projects\" RESTART IDENTITY CASCADE;"
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Projects.AnyAsync())
        {
            Console.WriteLine("⚠️  Database already has data. Skipping seed.");
            Console.WriteLine("    To reset: psql -U postgres -d taskflowpro -c \"TRUNCATE \\\"ActivityLogs\\\", \\\"Tasks\\\", \\\"TeamMembers\\\", \\\"Projects\\\" RESTART IDENTITY CASCADE;\"");
            return;
        }

        Console.WriteLine("🌱 Seeding TaskFlow Pro database...\n");

        var now = DateTime.UtcNow;

        // ── Team Members ───────────────────────────────────────────────────
        Console.WriteLine("  👥 Creating team members...");

        var members = new List<TeamMember>
        {
            new() { FullName = "Sarah Chen",       Email = "sarah.chen@company.dev",      IsActive = true  },
            new() { FullName = "Marcus Williams",  Email = "marcus.w@company.dev",         IsActive = true  },
            new() { FullName = "Priya Patel",      Email = "priya.patel@company.dev",      IsActive = true  },
            new() { FullName = "Jordan Lee",       Email = "jordan.lee@company.dev",       IsActive = true  },
            new() { FullName = "Alex Rivera",      Email = "alex.rivera@company.dev",      IsActive = true  },
            new() { FullName = "Emma Thompson",    Email = "emma.t@company.dev",           IsActive = true  },
            new() { FullName = "Daniel Okonkwo",   Email = "d.okonkwo@company.dev",        IsActive = true  },
            new() { FullName = "Sofia Andersson",  Email = "sofia.andersson@company.dev",  IsActive = false },
        };

        db.TeamMembers.AddRange(members);
        await db.SaveChangesAsync();

        var sarah  = members[0];
        var marcus = members[1];
        var priya  = members[2];
        var jordan = members[3];
        var alex   = members[4];
        var emma   = members[5];
        var daniel = members[6];

        db.ActivityLogs.AddRange(members.Select(m => new ActivityLog
        {
            EntityType    = "TeamMember",
            EntityId      = m.Id,
            TaskItemId    = 0,
            Action        = "MemberCreated",
            Actor         = "system",
            Details       = $"Team member '{m.FullName}' ({m.Email}) was added.",
            OccurredAtUtc = now.AddDays(-60),
        }));
        await db.SaveChangesAsync();

        Console.WriteLine($"     ✅ {members.Count} members created ({members.Count(m => m.IsActive)} active, {members.Count(m => !m.IsActive)} inactive)\n");

        // ── Projects ───────────────────────────────────────────────────────
        Console.WriteLine("  📁 Creating projects...");

        var projects = new List<Project>
        {
            new() { Name = "Customer Portal v2",    Code = "CPV2", IsArchived = false },
            new() { Name = "Payments Microservice", Code = "PAY",  IsArchived = false },
            new() { Name = "Mobile App — iOS",      Code = "IOS",  IsArchived = false },
            new() { Name = "Internal DevTools",     Code = "DEVT", IsArchived = false },
            new() { Name = "Legacy Monolith",       Code = "LGC",  IsArchived = true  },
        };

        db.Projects.AddRange(projects);
        await db.SaveChangesAsync();

        var portal   = projects[0];
        var payments = projects[1];
        var ios      = projects[2];
        var devtools = projects[3];
        var legacy   = projects[4];

        db.ActivityLogs.AddRange(projects.Select(p => new ActivityLog
        {
            EntityType    = "Project",
            EntityId      = p.Id,
            TaskItemId    = 0,
            Action        = "ProjectCreated",
            Actor         = "system",
            Details       = $"Project '{p.Name}' ({p.Code}) was created.",
            OccurredAtUtc = now.AddDays(-60),
        }));

        db.ActivityLogs.Add(new ActivityLog
        {
            EntityType    = "Project",
            EntityId      = legacy.Id,
            TaskItemId    = 0,
            Action        = "ProjectArchived",
            Actor         = "system",
            Details       = $"Project '{legacy.Name}' was archived.",
            OccurredAtUtc = now.AddDays(-10),
        });

        await db.SaveChangesAsync();

        Console.WriteLine($"     ✅ {projects.Count} projects created ({projects.Count(p => !p.IsArchived)} active, {projects.Count(p => p.IsArchived)} archived)\n");

        // ── Tasks ──────────────────────────────────────────────────────────
        Console.WriteLine("  ✅ Creating tasks...");

        var tasks = new List<TaskItem>
        {
            // ── Customer Portal v2 ─────────────────────────────────────────

            new()
            {
                Title          = "Redesign dashboard landing page",
                Description    = "Implement the new Figma design for the customer dashboard. Includes responsive layout, new KPI cards, and dark mode support.",
                Priority       = TaskPriority.High,
                Status         = TaskStatus.Done,
                ProjectId      = portal.Id,
                AssigneeId     = emma.Id,
                EstimatedHours = 16,
                ActualHours    = 18,
                DueDate        = now.AddDays(-20),
                CreatedAtUtc   = now.AddDays(-40),
                UpdatedAtUtc   = now.AddDays(-18),
            },
            new()
            {
                Title          = "Implement SSO with Google and Microsoft",
                Description    = "Add OAuth 2.0 / OpenID Connect login flow for Google Workspace and Microsoft 365 accounts. Include account linking for existing users.",
                Priority       = TaskPriority.Critical,
                Status         = TaskStatus.InProgress,
                ProjectId      = portal.Id,
                AssigneeId     = marcus.Id,
                EstimatedHours = 24,
                DueDate        = now.AddDays(7),
                CreatedAtUtc   = now.AddDays(-14),
                UpdatedAtUtc   = now.AddDays(-2),
            },
            new()
            {
                Title          = "Add CSV export to reports module",
                Description    = "Allow users to export filtered report data as CSV. Support UTF-8 BOM for Excel compatibility.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.Backlog,
                ProjectId      = portal.Id,
                AssigneeId     = priya.Id,
                EstimatedHours = 8,
                DueDate        = now.AddDays(21),
                CreatedAtUtc   = now.AddDays(-5),
                UpdatedAtUtc   = now.AddDays(-5),
            },
            new()
            {
                Title          = "Fix session timeout not logging out user",
                Description    = "When the JWT expires, the frontend continues to show the app rather than redirecting to login. Reproduce with 15-min token.",
                Priority       = TaskPriority.Critical,
                Status         = TaskStatus.Blocked,
                ProjectId      = portal.Id,
                AssigneeId     = sarah.Id,
                EstimatedHours = 4,
                DueDate        = now.AddDays(-3),
                CreatedAtUtc   = now.AddDays(-10),
                UpdatedAtUtc   = now.AddDays(-4),
            },
            new()
            {
                Title          = "Accessibility audit — WCAG 2.1 AA compliance",
                Description    = "Run axe-core and NVDA screen reader tests across all portal pages. Fix all critical and serious violations before Q3 release.",
                Priority       = TaskPriority.High,
                Status         = TaskStatus.Backlog,
                ProjectId      = portal.Id,
                EstimatedHours = 20,
                DueDate        = now.AddDays(-1),
                CreatedAtUtc   = now.AddDays(-8),
                UpdatedAtUtc   = now.AddDays(-8),
            },
            new()
            {
                Title          = "Write E2E tests for checkout flow",
                Description    = "Playwright tests covering: guest checkout, logged-in checkout, coupon application, payment failure, and order confirmation.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.Done,
                ProjectId      = portal.Id,
                AssigneeId     = jordan.Id,
                EstimatedHours = 12,
                ActualHours    = 14,
                DueDate        = now.AddDays(-15),
                CreatedAtUtc   = now.AddDays(-30),
                UpdatedAtUtc   = now.AddDays(-13),
            },

            // ── Fast-closed tasks ──────────────────────────────────────────

            new()
            {
                Title          = "Migrate old user preferences schema",
                Description    = "Discovered it was already handled by the portal v2 migration script.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.Done,
                ProjectId      = portal.Id,
                AssigneeId     = marcus.Id,
                EstimatedHours = 8,
                DueDate        = now.AddDays(-12),
                CreatedAtUtc   = now.AddDays(-15),
                UpdatedAtUtc   = now.AddDays(-14),
                IsFastClosed   = true,
                CloseReason    = "Duplicate",
            },
            new()
            {
                Title          = "Add dark mode toggle to settings page",
                Description    = "Deprioritized — dark mode is now handled globally via CSS variables.",
                Priority       = TaskPriority.Low,
                Status         = TaskStatus.Done,
                ProjectId      = portal.Id,
                AssigneeId     = emma.Id,
                EstimatedHours = 4,
                DueDate        = now.AddDays(-8),
                CreatedAtUtc   = now.AddDays(-10),
                UpdatedAtUtc   = now.AddDays(-9),
                IsFastClosed   = true,
                CloseReason    = "No Longer Needed",
            },
            new()
            {
                Title          = "Spike: evaluate GraphQL for reporting API",
                Description    = "Decision made to stay with REST after evaluating complexity vs. benefit.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.Done,
                ProjectId      = devtools.Id,
                AssigneeId     = daniel.Id,
                EstimatedHours = 16,
                DueDate        = now.AddDays(-5),
                CreatedAtUtc   = now.AddDays(-7),
                UpdatedAtUtc   = now.AddDays(-6),
                IsFastClosed   = true,
                CloseReason    = "Out of Scope",
            },

            // ── Payments Microservice ──────────────────────────────────────

            new()
            {
                Title          = "Integrate Stripe webhooks for payment events",
                Description    = "Handle payment_intent.succeeded, payment_intent.failed, and charge.refunded events. Store idempotency keys to prevent duplicate processing.",
                Priority       = TaskPriority.Critical,
                Status         = TaskStatus.InProgress,
                ProjectId      = payments.Id,
                AssigneeId     = alex.Id,
                EstimatedHours = 20,
                DueDate        = now.AddDays(5),
                CreatedAtUtc   = now.AddDays(-12),
                UpdatedAtUtc   = now.AddDays(-1),
            },
            new()
            {
                Title          = "Add retry logic for failed transactions",
                Description    = "Implement exponential backoff retry with jitter for transient payment failures. Max 3 retries, dead-letter queue after exhaustion.",
                Priority       = TaskPriority.High,
                Status         = TaskStatus.Backlog,
                ProjectId      = payments.Id,
                AssigneeId     = alex.Id,
                EstimatedHours = 16,
                DueDate        = now.AddDays(14),
                CreatedAtUtc   = now.AddDays(-7),
                UpdatedAtUtc   = now.AddDays(-7),
            },
            new()
            {
                Title          = "PCI-DSS compliance review",
                Description    = "Audit card data handling, encryption at rest and in transit. Ensure no PAN data is logged. Engage external QSA for attestation.",
                Priority       = TaskPriority.Critical,
                Status         = TaskStatus.Blocked,
                ProjectId      = payments.Id,
                AssigneeId     = daniel.Id,
                EstimatedHours = 40,
                DueDate        = now.AddDays(-5),
                CreatedAtUtc   = now.AddDays(-20),
                UpdatedAtUtc   = now.AddDays(-6),
            },
            new()
            {
                Title          = "Multi-currency support — EUR, GBP, CAD",
                Description    = "Add currency selection at checkout. Store prices in minor units. Integrate exchange rate API with daily refresh and fallback cache.",
                Priority       = TaskPriority.High,
                Status         = TaskStatus.Done,
                ProjectId      = payments.Id,
                AssigneeId     = priya.Id,
                EstimatedHours = 30,
                ActualHours    = 28,
                DueDate        = now.AddDays(-25),
                CreatedAtUtc   = now.AddDays(-50),
                UpdatedAtUtc   = now.AddDays(-22),
            },
            new()
            {
                Title          = "Load test payment endpoints",
                Description    = "k6 load test: 500 concurrent users, 10-minute ramp. Target: p95 < 300ms, error rate < 0.1%. Document results and any bottlenecks.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.Backlog,
                ProjectId      = payments.Id,
                EstimatedHours = 8,
                DueDate        = now.AddDays(30),
                CreatedAtUtc   = now.AddDays(-3),
                UpdatedAtUtc   = now.AddDays(-3),
            },

            // ── Mobile App — iOS ───────────────────────────────────────────

            new()
            {
                Title          = "SwiftUI migration — Home screen",
                Description    = "Migrate UIKit Home screen to SwiftUI. Use @StateObject for view model, LazyVGrid for product grid. Match pixel-perfect Figma spec.",
                Priority       = TaskPriority.High,
                Status         = TaskStatus.Done,
                ProjectId      = ios.Id,
                AssigneeId     = jordan.Id,
                EstimatedHours = 20,
                ActualHours    = 22,
                DueDate        = now.AddDays(-18),
                CreatedAtUtc   = now.AddDays(-35),
                UpdatedAtUtc   = now.AddDays(-16),
            },
            new()
            {
                Title          = "Push notifications — order status updates",
                Description    = "APNs integration for order shipped, delivered, and return initiated events. Support rich notifications with product image and deep link.",
                Priority       = TaskPriority.High,
                Status         = TaskStatus.InProgress,
                ProjectId      = ios.Id,
                AssigneeId     = sarah.Id,
                EstimatedHours = 18,
                DueDate        = now.AddDays(10),
                CreatedAtUtc   = now.AddDays(-9),
                UpdatedAtUtc   = now.AddDays(-1),
            },
            new()
            {
                Title          = "Biometric authentication (Face ID / Touch ID)",
                Description    = "Use LocalAuthentication framework. Fallback to PIN code. Store credentials in Keychain with kSecAttrAccessibleWhenUnlocked.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.Backlog,
                ProjectId      = ios.Id,
                AssigneeId     = jordan.Id,
                EstimatedHours = 12,
                DueDate        = now.AddDays(25),
                CreatedAtUtc   = now.AddDays(-4),
                UpdatedAtUtc   = now.AddDays(-4),
            },
            new()
            {
                Title          = "Fix crash on deep link from killed state",
                Description    = "App crashes with NSInternalInconsistencyException when opened via universal link from background-killed state. Reproducible on iOS 17.2+.",
                Priority       = TaskPriority.Critical,
                Status         = TaskStatus.InProgress,
                ProjectId      = ios.Id,
                AssigneeId     = emma.Id,
                EstimatedHours = 6,
                DueDate        = now.AddDays(2),
                CreatedAtUtc   = now.AddDays(-6),
                UpdatedAtUtc   = now.AddDays(-1),
            },
            new()
            {
                Title          = "App Store submission — v2.4.0",
                Description    = "Prepare release: update screenshots, write What's New copy, verify privacy manifest, run TestFlight regression, submit for review.",
                Priority       = TaskPriority.High,
                Status         = TaskStatus.Backlog,
                ProjectId      = ios.Id,
                AssigneeId     = emma.Id,
                EstimatedHours = 10,
                DueDate        = now.AddDays(18),
                CreatedAtUtc   = now.AddDays(-2),
                UpdatedAtUtc   = now.AddDays(-2),
            },

            // ── Internal DevTools ──────────────────────────────────────────

            new()
            {
                Title          = "CLI tool: database migration runner",
                Description    = "Internal dotnet tool to run, rollback, and list EF Core migrations across environments. Supports --dry-run and Slack notifications on completion.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.Done,
                ProjectId      = devtools.Id,
                AssigneeId     = marcus.Id,
                EstimatedHours = 14,
                ActualHours    = 12,
                DueDate        = now.AddDays(-30),
                CreatedAtUtc   = now.AddDays(-55),
                UpdatedAtUtc   = now.AddDays(-28),
            },
            new()
            {
                Title          = "Grafana dashboard — service health overview",
                Description    = "Create dashboards for: API latency percentiles, DB connection pool usage, queue depth, and error rates. Alert thresholds for SLOs.",
                Priority       = TaskPriority.Medium,
                Status         = TaskStatus.InProgress,
                ProjectId      = devtools.Id,
                AssigneeId     = daniel.Id,
                EstimatedHours = 16,
                DueDate        = now.AddDays(12),
                CreatedAtUtc   = now.AddDays(-11),
                UpdatedAtUtc   = now.AddDays(-2),
            },
            new()
            {
                Title          = "Automated dependency vulnerability scan",
                Description    = "GitHub Action to run `dotnet list package --vulnerable` and npm audit on every PR. Fail build on high/critical CVEs. Weekly full report to Slack.",
                Priority       = TaskPriority.Low,
                Status         = TaskStatus.Backlog,
                ProjectId      = devtools.Id,
                EstimatedHours = 6,
                DueDate        = now.AddDays(35),
                CreatedAtUtc   = now.AddDays(-1),
                UpdatedAtUtc   = now.AddDays(-1),
            },
        };

        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();

        var doneCount       = tasks.Count(t => t.Status == TaskStatus.Done);
        var inProgressCount = tasks.Count(t => t.Status == TaskStatus.InProgress);
        var blockedCount    = tasks.Count(t => t.Status == TaskStatus.Blocked);
        var backlogCount    = tasks.Count(t => t.Status == TaskStatus.Backlog);
        var fastClosedCount = tasks.Count(t => t.IsFastClosed);
        var overdueCount    = tasks.Count(t =>
            t.Status != TaskStatus.Done &&
            t.DueDate.HasValue &&
            t.DueDate.Value < now);

        Console.WriteLine($"     ✅ {tasks.Count} tasks created");
        Console.WriteLine($"        • Done: {doneCount}  |  In Progress: {inProgressCount}  |  Blocked: {blockedCount}  |  Backlog: {backlogCount}");
        Console.WriteLine($"        • Fast-closed: {fastClosedCount}  |  Overdue: {overdueCount}\n");

        // ── Activity Logs ──────────────────────────────────────────────────
        Console.WriteLine("  📋 Creating activity log...");

        var logs = new List<ActivityLog>();

        foreach (var task in tasks)
        {
            var titlePrefix = $"['{task.Title}'] ";

            // Creation event
            logs.Add(new ActivityLog
            {
                EntityType    = "Task",
                EntityId      = task.Id,
                TaskItemId    = task.Id,
                Action        = "TaskCreated",
                Actor         = "system",
                Details       = $"{titlePrefix}Task '{task.Title}' created in project id:{task.ProjectId}.",
                OccurredAtUtc = task.CreatedAtUtc,
            });

            // Initial assignee log
            if (task.AssigneeId.HasValue)
            {
                var assignee = members.First(m => m.Id == task.AssigneeId.Value);
                logs.Add(new ActivityLog
                {
                    EntityType    = "Task",
                    EntityId      = task.Id,
                    TaskItemId    = task.Id,
                    Action        = "AssigneeChanged",
                    Actor         = "system",
                    Details       = $"{titlePrefix}Task assigned to '{assignee.FullName}'.",
                    OccurredAtUtc = task.CreatedAtUtc.AddMinutes(2),
                });
            }

            // Status progression logs based on current state
            if (task.IsFastClosed)
            {
                logs.Add(new ActivityLog
                {
                    EntityType    = "Task",
                    EntityId      = task.Id,
                    TaskItemId    = task.Id,
                    Action        = "StatusChanged",
                    Actor         = "system",
                    Details       = $"{titlePrefix}Status changed from Backlog to Done. Reason: {task.CloseReason}.",
                    OccurredAtUtc = task.UpdatedAtUtc,
                });
            }
            else
            {
                if (task.Status == TaskStatus.InProgress || task.Status == TaskStatus.Done)
                {
                    logs.Add(new ActivityLog
                    {
                        EntityType    = "Task",
                        EntityId      = task.Id,
                        TaskItemId    = task.Id,
                        Action        = "StatusChanged",
                        Actor         = "system",
                        Details       = $"{titlePrefix}Status changed from Backlog to InProgress.",
                        OccurredAtUtc = task.CreatedAtUtc.AddDays(1),
                    });
                }

                if (task.Status == TaskStatus.Blocked)
                {
                    logs.Add(new ActivityLog
                    {
                        EntityType    = "Task",
                        EntityId      = task.Id,
                        TaskItemId    = task.Id,
                        Action        = "StatusChanged",
                        Actor         = "system",
                        Details       = $"{titlePrefix}Status changed from Backlog to InProgress.",
                        OccurredAtUtc = task.CreatedAtUtc.AddDays(1),
                    });
                    logs.Add(new ActivityLog
                    {
                        EntityType    = "Task",
                        EntityId      = task.Id,
                        TaskItemId    = task.Id,
                        Action        = "StatusChanged",
                        Actor         = "system",
                        Details       = $"{titlePrefix}Status changed from InProgress to Blocked. Reason: external dependency.",
                        OccurredAtUtc = task.CreatedAtUtc.AddDays(2),
                    });
                }

                if (task.Status == TaskStatus.Done)
                {
                    logs.Add(new ActivityLog
                    {
                        EntityType    = "Task",
                        EntityId      = task.Id,
                        TaskItemId    = task.Id,
                        Action        = "StatusChanged",
                        Actor         = "system",
                        Details       = $"{titlePrefix}Status changed from InProgress to Done.",
                        OccurredAtUtc = task.UpdatedAtUtc,
                    });
                }
            }
        }

        db.ActivityLogs.AddRange(logs);
        await db.SaveChangesAsync();

        Console.WriteLine($"     ✅ {logs.Count} activity log entries created\n");

        // ── Summary ────────────────────────────────────────────────────────
        Console.WriteLine("─────────────────────────────────────────");
        Console.WriteLine("🎉 Seed completed successfully!\n");
        Console.WriteLine("  Summary:");
        Console.WriteLine($"    Projects    : {projects.Count} ({projects.Count(p => p.IsArchived)} archived)");
        Console.WriteLine($"    Team Members: {members.Count} ({members.Count(m => !m.IsActive)} inactive)");
        Console.WriteLine($"    Tasks       : {tasks.Count} ({fastClosedCount} fast-closed)");
        Console.WriteLine($"    Activity Log: {logs.Count} entries");
        Console.WriteLine("\n  Run the app:");
        Console.WriteLine("    dotnet run --project src/TechChallenge.Web");
        Console.WriteLine("    http://localhost:5000\n");
    }
}