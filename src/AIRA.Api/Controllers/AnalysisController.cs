using System.Text.Json;
using AIRA.Core.Interfaces;
using AIRA.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AIRA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(IJobService jobService, ILogger<AnalysisController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new company analysis request
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<JobSubmissionResponse>> SubmitAnalysis([FromBody] AnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanySymbol))
        {
            return BadRequest(new { error = "CompanySymbol is required" });
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return BadRequest(new { error = "CompanyName is required" });
        }

        _logger.LogInformation(
            "Received analysis request for {CompanySymbol} ({CompanyName})",
            request.CompanySymbol,
            request.CompanyName);

        var job = await _jobService.CreateJobAsync(request);

        _logger.LogInformation("Created job {JobId} for {CompanySymbol}", job.Id, request.CompanySymbol);

        return Accepted(new JobSubmissionResponse
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            Message = $"Analysis job created for {request.CompanyName} ({request.CompanySymbol})",
            StatusUrl = $"/api/analysis/{job.Id}"
        });
    }

    /// <summary>
    /// Get the status and result of an analysis job
    /// </summary>
    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<JobStatusResponse>> GetJobStatus(Guid jobId)
    {
        var job = await _jobService.GetJobAsync(jobId);

        if (job == null)
        {
            return NotFound(new { error = $"Job {jobId} not found" });
        }

        var response = new JobStatusResponse
        {
            JobId = job.Id,
            CompanySymbol = job.CompanySymbol,
            CompanyName = job.CompanyName,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage,
            StepsCompleted = job.Steps.Count
        };

        // Include result if completed
        if (job.Status == JobStatus.Completed && !string.IsNullOrEmpty(job.ResultJson))
        {
            response.Result = JsonSerializer.Deserialize<AnalysisResult>(job.ResultJson);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get the observable agent steps for a job
    /// </summary>
    [HttpGet("{jobId:guid}/steps")]
    public async Task<ActionResult<IEnumerable<AgentStep>>> GetJobSteps(Guid jobId)
    {
        var job = await _jobService.GetJobAsync(jobId);

        if (job == null)
        {
            return NotFound(new { error = $"Job {jobId} not found" });
        }

        return Ok(job.Steps);
    }
}

public class JobSubmissionResponse
{
    public Guid JobId { get; set; }
    public required string Status { get; set; }
    public required string Message { get; set; }
    public required string StatusUrl { get; set; }
}

public class JobStatusResponse
{
    public Guid JobId { get; set; }
    public required string CompanySymbol { get; set; }
    public required string CompanyName { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int StepsCompleted { get; set; }
    public AnalysisResult? Result { get; set; }
}
