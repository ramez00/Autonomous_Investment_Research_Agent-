using AIRA.Agents.Agents;
using AIRA.Core.Interfaces;
using AIRA.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIRA.Agents.Orchestration;

/// <summary>
/// Orchestrates the multi-agent research workflow
/// </summary>
public class ResearchOrchestrator : IResearchOrchestrator
{
    private readonly PlannerAgent _planner;
    private readonly FinancialDataAgent _financialAgent;
    private readonly NewsAnalystAgent _newsAgent;
    private readonly SynthesizerAgent _synthesizer;
    private readonly ILogger<ResearchOrchestrator> _logger;

    public ResearchOrchestrator(
        PlannerAgent planner,
        FinancialDataAgent financialAgent,
        NewsAnalystAgent newsAgent,
        SynthesizerAgent synthesizer,
        ILogger<ResearchOrchestrator> logger)
    {
        _planner = planner;
        _financialAgent = financialAgent;
        _newsAgent = newsAgent;
        _synthesizer = synthesizer;
        _logger = logger;
    }

    /// <summary>
    /// Executes the full research pipeline for a company
    /// </summary>
    public async Task<AnalysisResult> ExecuteResearchAsync(
        AnalysisJob job,
        Func<AgentStep, Task>? onStepCompleted = null,
        CancellationToken cancellationToken = default)
    {
        var allSteps = new List<AgentStep>();
        var stepNumber = 0;

        // Helper to track steps across all agents
        async Task TrackStep(AgentStep step)
        {
            allSteps.Add(step);
            if (onStepCompleted != null)
            {
                await onStepCompleted(step);
            }
        }

        _logger.LogInformation(
            "Starting research orchestration for {Symbol} ({CompanyName})",
            job.CompanySymbol, job.CompanyName);

        try
        {
            // Phase 1: Planning
            _logger.LogInformation("Phase 1: Creating research plan");
            _planner.SetStepCallback(TrackStep, stepNumber);
            
            var plan = await _planner.CreateResearchPlanAsync(
                job.CompanyName,
                job.CompanySymbol,
                job.AnalysisDepth,
                cancellationToken);
            
            stepNumber = _planner.CurrentStepCount;

            // Phase 2: Parallel data gathering
            _logger.LogInformation("Phase 2: Gathering data in parallel");
            
            _financialAgent.SetStepCallback(TrackStep, stepNumber);
            _newsAgent.SetStepCallback(TrackStep, stepNumber);

            // Execute financial and news analysis in parallel
            var financialTask = _financialAgent.GatherFinancialDataAsync(
                job.CompanySymbol,
                job.CompanyName,
                plan,
                cancellationToken);

            var newsTask = _newsAgent.AnalyzeNewsAsync(
                job.CompanyName,
                job.CompanySymbol,
                plan,
                cancellationToken);

            await Task.WhenAll(financialTask, newsTask);

            var financialAnalysis = await financialTask;
            var newsAnalysis = await newsTask;

            stepNumber = Math.Max(_financialAgent.CurrentStepCount, _newsAgent.CurrentStepCount);

            // Phase 3: Synthesis
            _logger.LogInformation("Phase 3: Synthesizing findings");
            _synthesizer.SetStepCallback(TrackStep, stepNumber);

            var result = await _synthesizer.SynthesizeAsync(
                job.CompanyName,
                job.CompanySymbol,
                plan,
                financialAnalysis,
                newsAnalysis,
                allSteps,
                cancellationToken);

            _logger.LogInformation(
                "Research completed for {Symbol}: Signal={Signal}, Confidence={Confidence:P0}",
                job.CompanySymbol, result.Signal, result.Confidence);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Research was cancelled for {Symbol}", job.CompanySymbol);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Research failed for {Symbol}", job.CompanySymbol);
            
            // Record the failure
            await TrackStep(new AgentStep
            {
                StepNumber = stepNumber + 1,
                AgentName = "Orchestrator",
                Action = "Research failed",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow,
                IsSuccess = false,
                ErrorMessage = ex.Message
            });

            throw;
        }
    }
}
