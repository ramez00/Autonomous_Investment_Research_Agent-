namespace AIRA.Core.Models;

/// <summary>
/// Represents an analysis job with its current state
/// </summary>
public class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string CompanySymbol { get; set; }
    
    public required string CompanyName { get; set; }
    
    public string AnalysisDepth { get; set; } = "standard";
    
    public JobStatus Status { get; set; } = JobStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Serialized JSON result when analysis is complete
    /// </summary>
    public string? ResultJson { get; set; }
    
    /// <summary>
    /// Observable agent steps for transparency
    /// </summary>
    public List<AgentStep> Steps { get; set; } = new();
}

public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
