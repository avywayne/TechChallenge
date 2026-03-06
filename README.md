# TaskFlow Pro – Starter (PostgreSQL)

Starter scaffold for the extended challenge.

## Prerequisites
- .NET SDK 8+
- PostgreSQL (local or container)
- dotnet-ef tool (`dotnet tool install --global dotnet-ef`)

## Configure connection string
Set environment variable:

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=taskflow_pro;Username=postgres;Password=postgres"
```

(Windows PowerShell)
```powershell
$env:ConnectionStrings__Default="Host=localhost;Port=5432;Database=taskflow_pro;Username=postgres;Password=postgres"
```

## Setup
```bash
dotnet restore
cd src/TechChallenge.Web
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

## Included in starter
- Basic models for Project, TeamMember, TaskItem, ActivityLog
- DbContext + baseline indexes/constraints
- Service interface + minimal implementation shell
- Placeholder Tasks page

## Candidate TODO
- Complete full CRUD and state transitions
- Implement dashboard and audit trail behavior
- Improve UX, validation, and accessibility
- Add robust filtering/search/sorting flows
