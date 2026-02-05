using AIRA.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AIRA.Infrastructure.Persistence;

public class JobDbContext : DbContext
{
    public JobDbContext(DbContextOptions<JobDbContext> options) : base(options)
    {
    }

    public DbSet<JobEntity> Jobs => Set<JobEntity>();
    public DbSet<AgentStepEntity> AgentSteps => Set<AgentStepEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanySymbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AnalysisDepth).HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired();
            entity.HasMany(e => e.Steps)
                .WithOne(s => s.Job)
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentStepEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgentName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(500);
        });
    }
}

public class JobEntity
{
    public Guid Id { get; set; }
    public required string CompanySymbol { get; set; }
    public required string CompanyName { get; set; }
    public string AnalysisDepth { get; set; } = "standard";
    public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultJson { get; set; }
    public List<AgentStepEntity> Steps { get; set; } = new();
}

public class AgentStepEntity
{
    public int Id { get; set; }
    public Guid JobId { get; set; }
    public int StepNumber { get; set; }
    public required string AgentName { get; set; }
    public required string Action { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public long? DurationMs { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public JobEntity Job { get; set; } = null!;
}
