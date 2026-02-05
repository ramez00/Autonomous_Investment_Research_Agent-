using System.Threading.Channels;
using AIRA.Core.Interfaces;
using AIRA.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIRA.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes analysis jobs from the queue
/// </summary>
public class JobProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobProcessorService> _logger;
    private readonly Channel<Guid> _jobQueue;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public JobProcessorService(
        IServiceProvider serviceProvider,
        ILogger<JobProcessorService> logger,
        Channel<Guid> jobQueue)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _jobQueue = jobQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Processor Service started");

        // Start two tasks: one for polling pending jobs, one for processing queue
        var pollingTask = PollPendingJobsAsync(stoppingToken);
        var processingTask = ProcessQueueAsync(stoppingToken);

        await Task.WhenAll(pollingTask, processingTask);
    }

    private async Task PollPendingJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

                var pendingJobs = await repository.GetPendingJobsAsync();
                foreach (var job in pendingJobs)
                {
                    await _jobQueue.Writer.WriteAsync(job.Id, stoppingToken);
                    _logger.LogDebug("Queued job {JobId} for processing", job.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling for pending jobs");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _jobQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", jobId);
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IResearchOrchestrator>();

        var job = await jobService.GetJobAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        // Skip if already processed
        if (job.Status != JobStatus.Pending)
        {
            _logger.LogDebug("Job {JobId} already has status {Status}, skipping", jobId, job.Status);
            return;
        }

        _logger.LogInformation("Starting analysis for job {JobId}: {CompanySymbol}", jobId, job.CompanySymbol);

        try
        {
            await jobService.MarkJobStartedAsync(jobId);

            var result = await orchestrator.ExecuteResearchAsync(
                job,
                async step =>
                {
                    await jobService.AddStepAsync(jobId, step);
                    _logger.LogDebug(
                        "Job {JobId} step {StepNumber}: {Agent} - {Action}",
                        jobId, step.StepNumber, step.AgentName, step.Action);
                },
                stoppingToken);

            await jobService.MarkJobCompletedAsync(jobId, result);

            _logger.LogInformation(
                "Completed analysis for job {JobId}: {CompanySymbol} with signal {Signal}",
                jobId, job.CompanySymbol, result.Signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed for job {JobId}", jobId);
            await jobService.MarkJobFailedAsync(jobId, ex.Message);
        }
    }
}
