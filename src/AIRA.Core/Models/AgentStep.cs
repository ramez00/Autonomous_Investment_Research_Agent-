namespace AIRA.Core.Models;

/// <summary>
/// Represents a single step taken by an agent during analysis
/// </summary>
public class AgentStep
{
    public int StepNumber { get; set; }
    
    public required string AgentName { get; set; }
    
    public required string Action { get; set; }
    
    public string? Details { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public TimeSpan? Duration { get; set; }
    
    public bool IsSuccess { get; set; } = true;
    
    public string? ErrorMessage { get; set; }
}
