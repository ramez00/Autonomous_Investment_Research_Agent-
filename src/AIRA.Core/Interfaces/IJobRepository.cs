using AIRA.Core.Models;

namespace AIRA.Core.Interfaces;

/// <summary>
/// Repository interface for job persistence
/// </summary>
public interface IJobRepository
{
    Task<IEnumerable<AnalysisJob>> GetAllAsync();
    Task<AnalysisJob> CreateAsync(AnalysisJob job);
    Task<AnalysisJob?> GetByIdAsync(Guid id);
    Task<AnalysisJob> UpdateAsync(AnalysisJob job);
    Task<IEnumerable<AnalysisJob>> GetPendingJobsAsync();
    Task AddStepAsync(Guid jobId, AgentStep step);
}
