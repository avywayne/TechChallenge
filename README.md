# TaskFlow Pro

A task management web application built with Blazor Server, PostgreSQL, and Entity Framework Core.

## Tech Stack

- **Framework:** ASP.NET Core 9 / Blazor Server
- **Database:** PostgreSQL 15+
- **ORM:** Entity Framework Core 9
- **Testing:** xUnit + FluentAssertions
- **Styling:** Custom CSS (dark theme)

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/avywayne/TechChallenge.git
cd /starter
```

### 2. Create the database

```bash
psql -U postgres -c "CREATE DATABASE taskflowpro;"
```

### 3. Configure the connection string

The default connection string in `src/TechChallenge.Web/appsettings.json` is:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=taskflowpro;Username=postgres;Password=postgres"
  }
}
```

Update the `Username` and `Password` values to match your local PostgreSQL credentials if needed.

### 4. Apply migrations

```bash
dotnet ef database update --project src/TechChallenge.Web
```

### 5. Seed the database (optional but recommended)

```bash
dotnet run --project src/TechChallenge.Web -- --seed
```

This creates:
- 5 projects (4 active, 1 archived)
- 8 team members (7 active, 1 inactive)
- 22 tasks across all states including fast-closed examples
- Full activity log history

To reset and re-seed:

```bash
psql -U postgres -d taskflowpro -c "TRUNCATE \"ActivityLogs\", \"Tasks\", \"TeamMembers\", \"Projects\" RESTART IDENTITY CASCADE;"
dotnet run --project src/TechChallenge.Web -- --seed
```

### 6. Run the application

```bash
dotnet run --project src/TechChallenge.Web
```

Open [http://localhost:5000](http://localhost:5000) in your browser.

---

## Running Tests

```bash
dotnet test
```

Expected output: **57 tests passing, 0 failing**.

Test coverage includes:
- `TaskStateMachineTests` — state transition rules, fast-close, reason requirements
- `TaskServiceTests` — CRUD, pagination, search, audit logging
- `TaskServiceExtendedTests` — fast-close, edit restrictions, subtasks
- `PagedResultTests` — pagination helpers

---

## Features

### Task Management
- Create, edit, and delete tasks with title, description, priority, project, assignee, due date, and hour tracking
- Kanban board view and table view with sorting and filtering
- Subtask support with status cycling

### State Machine
Tasks follow a strict state machine with enforced transition rules:

| From | To | Requires Reason | Fast-Close |
|------|----|-----------------|------------|
| Backlog | In Progress | No | No |
| Backlog | Blocked | Yes | ✅ |
| Backlog | Done | Yes | ✅ |
| In Progress | Blocked | No | No |
| In Progress | Done | Yes | ✅ |
| Blocked | Done | No | No |
| Done | Backlog | No | No |

Blocked transitions: In Progress → Backlog, Blocked → Backlog, Blocked → In Progress, Done → In Progress, Done → Blocked.

### Fast-Close Tracking
Tasks closed without following the normal workflow are marked with ⚡ Fast-closed and a reason. The dashboard shows a dedicated KPI and table for fast-closed tasks.

### Edit Restrictions
- **Done tasks** cannot be edited at all
- **Blocked tasks** can only have their title and description edited

### Projects & Team Members
- Create, edit, archive, and delete projects
- Create, edit, activate/deactivate, and delete team members
- Cascade delete: removing a project or member also removes their associated tasks

### Activity Log
Full audit trail for all entities (tasks, projects, team members) with entity type filtering and action filtering.

### Dashboard
- KPI cards: Open Tasks, Overdue, Blocked, Due This Week, Fast-closed
- Task distribution donut chart
- Recent tasks and active projects overview
- Fast-closed tasks table
- All KPIs exclude archived projects and inactive members

---

## Project Structure

```
starter/
├── src/
│   └── TechChallenge.Web/
│       ├── Data/               # DbContext, services, seed data
│       ├── Domain/             # TaskStateMachine, business logic
│       ├── Models/             # Entity models
│       ├── Pages/              # Blazor pages (Dashboard, Tasks, Projects, Members, Activity)
│       ├── Shared/             # Layout, NavMenu, shared components
│       └── wwwroot/            # Static assets (CSS, JS)
└── tests/
    └── TechChallenge.Tests/
        ├── Domain/             # State machine tests
        └── Services/           # Service integration tests
```
