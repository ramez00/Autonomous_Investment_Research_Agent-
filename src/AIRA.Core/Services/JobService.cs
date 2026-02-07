using System.Text.Json;
using AIRA.Core.Interfaces;
using AIRA.Core.Models;

namespace AIRA.Core.Services;

/// <summary>
/// Implementation of job management service
/// </summary>
public class JobService : IJobService
{
    private readonly IJobRepository _repository;

    public JobService(IJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnalysisJob> CreateJobAsync(AnalysisRequest request)
    {
        var job = new AnalysisJob
        {
            CompanySymbol = request.CompanySymbol,
            CompanyName = request.CompanyName,
            AnalysisDepth = request.AnalysisDepth,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(job);
    }

    public async Task<AnalysisJob?> GetJobAsync(Guid jobId)
    {
        return await _repository.GetByIdAsync(jobId);
    }

    public async Task<IEnumerable<AgentStep>> GetJobStepsAsync(Guid jobId)
    {
        var job = await _repository.GetByIdAsync(jobId);
        return job?.Steps ?? Enumerable.Empty<AgentStep>();
    }

    public async Task AddStepAsync(Guid jobId, AgentStep step)
    {
        await _repository.AddStepAsync(jobId, step);
    }

    public async Task MarkJobStartedAsync(Guid jobId)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Status = JobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(job);
        }
    }

    public async Task MarkJobCompletedAsync(Guid jobId, AnalysisResult result)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ResultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await _repository.UpdateAsync(job);
        }
    }

    public async Task MarkJobFailedAsync(Guid jobId, string errorMessage)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Status = JobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = errorMessage;
            await _repository.UpdateAsync(job);
        }
    }

    public async Task<IEnumerable<AnalysisJob>> GetAllJobsAsync()
    {
        return await _repository.GetAllAsync();
    }
}
