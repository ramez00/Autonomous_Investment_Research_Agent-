using AIRA.Core.Models;

namespace AIRA.Core.Interfaces;

/// <summary>
/// Orchestrates the multi-agent research workflow
/// </summary>
public interface IResearchOrchestrator
{
    /// <summary>
    /// Executes the full research pipeline for a company
    /// </summary>
    /// <param name="job">The analysis job to process</param>
    /// <param name="onStepCompleted">Callback invoked when an agent step completes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The analysis result</returns>
    Task<AnalysisResult> ExecuteResearchAsync(
        AnalysisJob job,
        Func<AgentStep, Task>? onStepCompleted = null,
        CancellationToken cancellationToken = default);
}
