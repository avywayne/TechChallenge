using Microsoft.EntityFrameworkCore;
using TechChallenge.Web.Models;

namespace TechChallenge.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    
    {
        base.OnModelCreating(modelBuilder);

       modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Code).IsUnique();

            // Deleting a project deletes all its tasks
            entity.HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();

            // Deleting a member deletes all their assigned tasks
            entity.HasMany(m => m.AssignedTasks)
                .WithOne(t => t.Assignee)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Priority);
            entity.HasIndex(x => x.DueDate);
            entity.HasIndex(x => x.ProjectId);
            entity.HasIndex(x => x.AssigneeId);
            
            // Optimistic concurrency — maps to PostgreSQL xmin system column
            // xmin is automatically updated by Postgres on every row modification
            entity.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .IsRowVersion()
                .ValueGeneratedOnAddOrUpdate();
                 
            // Self-referencing relationship for subtasks
            entity.HasOne(t => t.Parent)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a task deletes its subtasks     
                    });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Actor).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.TaskItemId);
            entity.HasIndex(x => x.OccurredAtUtc);
        });
    }
}
