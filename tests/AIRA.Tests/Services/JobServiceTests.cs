using AIRA.Core.Interfaces;
using AIRA.Core.Models;
using AIRA.Core.Services;
using Moq;

namespace AIRA.Tests.Services;

public class JobServiceTests
{
    private readonly Mock<IJobRepository> _mockRepository;
    private readonly JobService _jobService;

    public JobServiceTests()
    {
        _mockRepository = new Mock<IJobRepository>();
        _jobService = new JobService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateJobAsync_ShouldCreateJobWithPendingStatus()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            CompanySymbol = "AAPL",
            CompanyName = "Apple Inc.",
            AnalysisDepth = "standard"
        };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<AnalysisJob>()))
            .ReturnsAsync((AnalysisJob job) => job);

        // Act
        var result = await _jobService.CreateJobAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AAPL", result.CompanySymbol);
        Assert.Equal("Apple Inc.", result.CompanyName);
        Assert.Equal("standard", result.AnalysisDepth);
        Assert.Equal(JobStatus.Pending, result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
        
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<AnalysisJob>()), Times.Once);
    }

    [Fact]
    public async Task GetJobAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new AnalysisJob
        {
            Id = jobId,
            CompanySymbol = "MSFT",
            CompanyName = "Microsoft",
            Status = JobStatus.Running
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        // Act
        var result = await _jobService.GetJobAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jobId, result.Id);
        Assert.Equal("MSFT", result.CompanySymbol);
    }

    [Fact]
    public async Task GetJobAsync_NonExistingJob_ReturnsNull()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync((AnalysisJob?)null);

        // Act
        var result = await _jobService.GetJobAsync(jobId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MarkJobStartedAsync_UpdatesStatusAndStartedAt()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new AnalysisJob
        {
            Id = jobId,
            CompanySymbol = "AAPL",
            CompanyName = "Apple Inc.",
            Status = JobStatus.Pending
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AnalysisJob>()))
            .ReturnsAsync((AnalysisJob j) => j);

        // Act
        await _jobService.MarkJobStartedAsync(jobId);

        // Assert
        Assert.Equal(JobStatus.Running, job.Status);
        Assert.NotNull(job.StartedAt);
        
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<AnalysisJob>(j => 
            j.Status == JobStatus.Running && j.StartedAt != null)), Times.Once);
    }

    [Fact]
    public async Task MarkJobCompletedAsync_UpdatesStatusAndResultJson()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new AnalysisJob
        {
            Id = jobId,
            CompanySymbol = "AAPL",
            CompanyName = "Apple Inc.",
            Status = JobStatus.Running
        };

        var result = new AnalysisResult
        {
            Company = "Apple Inc. (AAPL)",
            Thesis = "Strong buy",
            Signal = "BULLISH",
            Confidence = 0.85
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AnalysisJob>()))
            .ReturnsAsync((AnalysisJob j) => j);

        // Act
        await _jobService.MarkJobCompletedAsync(jobId, result);

        // Assert
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.NotNull(job.ResultJson);
        Assert.Contains("BULLISH", job.ResultJson);
    }

    [Fact]
    public async Task MarkJobFailedAsync_UpdatesStatusAndErrorMessage()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new AnalysisJob
        {
            Id = jobId,
            CompanySymbol = "INVALID",
            CompanyName = "Invalid Company",
            Status = JobStatus.Running
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AnalysisJob>()))
            .ReturnsAsync((AnalysisJob j) => j);

        // Act
        await _jobService.MarkJobFailedAsync(jobId, "API rate limit exceeded");

        // Assert
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal("API rate limit exceeded", job.ErrorMessage);
    }

    [Fact]
    public async Task GetJobStepsAsync_ReturnsStepsFromJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var steps = new List<AgentStep>
        {
            new() { StepNumber = 1, AgentName = "Planner", Action = "Created plan" },
            new() { StepNumber = 2, AgentName = "FinancialData", Action = "Fetched data" }
        };

        var job = new AnalysisJob
        {
            Id = jobId,
            CompanySymbol = "AAPL",
            CompanyName = "Apple Inc.",
            Steps = steps
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        // Act
        var result = await _jobService.GetJobStepsAsync(jobId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.AgentName == "Planner");
        Assert.Contains(result, s => s.AgentName == "FinancialData");
    }
}
