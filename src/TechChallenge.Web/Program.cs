using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Data;
using TechChallenge.Web.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();

// Singleton — stateless rules shared across all requests
builder.Services.AddSingleton<TaskStateMachine>();

var app = builder.Build();

// ── Seed command ───────────────────────────────────────────────────────────
// Run with: dotnet run --project src/TechChallenge.Web -- --seed
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(db);
    return; // Exit after seeding — do not start the web server
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();