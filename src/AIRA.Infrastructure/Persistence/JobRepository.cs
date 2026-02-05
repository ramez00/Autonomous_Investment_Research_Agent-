using AIRA.Core.Interfaces;
using AIRA.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AIRA.Infrastructure.Persistence;

public class JobRepository : IJobRepository
{
    private readonly JobDbContext _context;

    public JobRepository(JobDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisJob> CreateAsync(AnalysisJob job)
    {
        var entity = new JobEntity
        {
            Id = job.Id,
            CompanySymbol = job.CompanySymbol,
            CompanyName = job.CompanyName,
            AnalysisDepth = job.AnalysisDepth,
            Status = job.Status,
            CreatedAt = job.CreatedAt
        };

        _context.Jobs.Add(entity);
        await _context.SaveChangesAsync();

        return job;
    }

    public async Task<AnalysisJob?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Jobs
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (entity == null) return null;

        return MapToModel(entity);
    }

    public async Task<AnalysisJob> UpdateAsync(AnalysisJob job)
    {
        var entity = await _context.Jobs.FindAsync(job.Id);
        if (entity == null)
        {
            throw new InvalidOperationException($"Job {job.Id} not found");
        }

        entity.Status = job.Status;
        entity.StartedAt = job.StartedAt;
        entity.CompletedAt = job.CompletedAt;
        entity.ErrorMessage = job.ErrorMessage;
        entity.ResultJson = job.ResultJson;

        await _context.SaveChangesAsync();

        return job;
    }

    public async Task<IEnumerable<AnalysisJob>> GetPendingJobsAsync()
    {
        var entities = await _context.Jobs
            .Include(j => j.Steps)
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();

        return entities.Select(MapToModel);
    }

    public async Task AddStepAsync(Guid jobId, AgentStep step)
    {
        var stepEntity = new AgentStepEntity
        {
            JobId = jobId,
            StepNumber = step.StepNumber,
            AgentName = step.AgentName,
            Action = step.Action,
            Details = step.Details,
            Timestamp = step.Timestamp,
            DurationMs = step.Duration?.Milliseconds,
            IsSuccess = step.IsSuccess,
            ErrorMessage = step.ErrorMessage
        };

        _context.AgentSteps.Add(stepEntity);
        await _context.SaveChangesAsync();
    }

    private static AnalysisJob MapToModel(JobEntity entity)
    {
        return new AnalysisJob
        {
            Id = entity.Id,
            CompanySymbol = entity.CompanySymbol,
            CompanyName = entity.CompanyName,
            AnalysisDepth = entity.AnalysisDepth,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            ErrorMessage = entity.ErrorMessage,
            ResultJson = entity.ResultJson,
            Steps = entity.Steps.Select(s => new AgentStep
            {
                StepNumber = s.StepNumber,
                AgentName = s.AgentName,
                Action = s.Action,
                Details = s.Details,
                Timestamp = s.Timestamp,
                Duration = s.DurationMs.HasValue ? TimeSpan.FromMilliseconds(s.DurationMs.Value) : null,
                IsSuccess = s.IsSuccess,
                ErrorMessage = s.ErrorMessage
            }).OrderBy(s => s.StepNumber).ToList()
        };
    }
}
