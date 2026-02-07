using AIRA.Core.Models;

namespace AIRA.Core.Interfaces;

/// <summary>
/// Service interface for managing analysis jobs
/// </summary>
public interface IJobService
{
    Task<IEnumerable<AnalysisJob>> GetAllJobsAsync();
    /// <summary>
    /// Creates a new analysis job and queues it for processing
    /// </summary>
    Task<AnalysisJob> CreateJobAsync(AnalysisRequest request);

    /// <summary>
    /// Gets the current status and result of a job
    /// </summary>
    Task<AnalysisJob?> GetJobAsync(Guid jobId);

    /// <summary>
    /// Gets the observable steps for a job
    /// </summary>
    Task<IEnumerable<AgentStep>> GetJobStepsAsync(Guid jobId);

    /// <summary>
    /// Adds a step to the job's trace log
    /// </summary>
    Task AddStepAsync(Guid jobId, AgentStep step);

    /// <summary>
    /// Marks job as started
    /// </summary>
    Task MarkJobStartedAsync(Guid jobId);

    /// <summary>
    /// Marks job as completed with result
    /// </summary>
    Task MarkJobCompletedAsync(Guid jobId, AnalysisResult result);

    /// <summary>
    /// Marks job as failed with error
    /// </summary>
    Task MarkJobFailedAsync(Guid jobId, string errorMessage);
}
