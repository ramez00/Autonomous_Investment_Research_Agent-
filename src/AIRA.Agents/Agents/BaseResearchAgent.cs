using AIRA.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIRA.Agents.Agents;

/// <summary>
/// Base class for research agents providing common functionality
/// </summary>
public abstract class BaseResearchAgent
{
    protected readonly ILogger Logger;
    private int _stepCounter;
    private Func<AgentStep, Task>? _onStepCompleted;

    public abstract string AgentName { get; }

    protected BaseResearchAgent(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Set the callback for when a step is completed
    /// </summary>
    public void SetStepCallback(Func<AgentStep, Task>? callback, int startingStep = 0)
    {
        _onStepCompleted = callback;
        _stepCounter = startingStep;
    }

    /// <summary>
    /// Records a step taken by this agent
    /// </summary>
    protected async Task RecordStepAsync(
        string action,
        string? details = null,
        bool isSuccess = true,
        string? errorMessage = null,
        TimeSpan? duration = null)
    {
        // Thread-safe increment
        var stepNumber = System.Threading.Interlocked.Increment(ref _stepCounter);
        
        var step = new AgentStep
        {
            StepNumber = stepNumber,
            AgentName = AgentName,
            Action = action,
            Details = details,
            Timestamp = DateTime.UtcNow,
            Duration = duration,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage
        };

        Logger.LogInformation(
            "[{Agent}] Step {StepNumber}: {Action}",
            AgentName, stepNumber, action);

        if (_onStepCompleted != null)
        {
            await _onStepCompleted(step);
        }
    }

    /// <summary>
    /// Gets the current step counter for coordination
    /// </summary>
    public int CurrentStepCount => _stepCounter;
}
