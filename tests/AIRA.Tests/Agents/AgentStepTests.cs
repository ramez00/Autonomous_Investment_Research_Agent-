using AIRA.Core.Models;

namespace AIRA.Tests.Agents;

public class AgentStepTests
{
    [Fact]
    public void AgentStep_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var step = new AgentStep
        {
            StepNumber = 1,
            AgentName = "TestAgent",
            Action = "Test action"
        };

        // Assert
        Assert.Equal(1, step.StepNumber);
        Assert.Equal("TestAgent", step.AgentName);
        Assert.Equal("Test action", step.Action);
        Assert.True(step.IsSuccess);
        Assert.Null(step.ErrorMessage);
        Assert.Null(step.Duration);
        Assert.True(step.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void AgentStep_CanRecordFailure()
    {
        // Arrange & Act
        var step = new AgentStep
        {
            StepNumber = 5,
            AgentName = "FinancialData",
            Action = "Fetch stock data",
            IsSuccess = false,
            ErrorMessage = "API rate limit exceeded"
        };

        // Assert
        Assert.False(step.IsSuccess);
        Assert.Equal("API rate limit exceeded", step.ErrorMessage);
    }

    [Fact]
    public void AgentStep_CanRecordDuration()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(2.5);

        // Act
        var step = new AgentStep
        {
            StepNumber = 3,
            AgentName = "NewsAnalyst",
            Action = "Analyze sentiment",
            Duration = duration
        };

        // Assert
        Assert.NotNull(step.Duration);
        Assert.Equal(2500, step.Duration.Value.TotalMilliseconds);
    }

    [Fact]
    public void AgentStepSummary_ContainsKeyFields()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 4, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var summary = new AgentStepSummary
        {
            Agent = "Synthesizer",
            Action = "Generated investment thesis",
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal("Synthesizer", summary.Agent);
        Assert.Equal("Generated investment thesis", summary.Action);
        Assert.Equal(timestamp, summary.Timestamp);
    }

    [Fact]
    public void AnalysisJob_TracksMultipleSteps()
    {
        // Arrange
        var job = new AnalysisJob
        {
            CompanySymbol = "AAPL",
            CompanyName = "Apple Inc."
        };

        // Act
        job.Steps.Add(new AgentStep { StepNumber = 1, AgentName = "Planner", Action = "Created plan" });
        job.Steps.Add(new AgentStep { StepNumber = 2, AgentName = "FinancialData", Action = "Fetched data" });
        job.Steps.Add(new AgentStep { StepNumber = 3, AgentName = "NewsAnalyst", Action = "Analyzed news" });
        job.Steps.Add(new AgentStep { StepNumber = 4, AgentName = "Synthesizer", Action = "Generated thesis" });

        // Assert
        Assert.Equal(4, job.Steps.Count);
        Assert.Equal("Planner", job.Steps[0].AgentName);
        Assert.Equal("Synthesizer", job.Steps[3].AgentName);
    }
}
