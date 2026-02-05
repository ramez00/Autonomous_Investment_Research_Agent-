using System.Text.Json.Serialization;

namespace AIRA.Core.Models;

/// <summary>
/// The final structured output of an investment analysis
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Company identifier (e.g., "Apple Inc. (AAPL)")
    /// </summary>
    [JsonPropertyName("company")]
    public required string Company { get; set; }

    /// <summary>
    /// Synthesized investment thesis
    /// </summary>
    [JsonPropertyName("thesis")]
    public required string Thesis { get; set; }

    /// <summary>
    /// Directional signal: BULLISH, BEARISH, or NEUTRAL
    /// </summary>
    [JsonPropertyName("signal")]
    public required string Signal { get; set; }

    /// <summary>
    /// Confidence score between 0 and 1
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// Key actionable insights categorized by type
    /// </summary>
    [JsonPropertyName("insights")]
    public List<Insight> Insights { get; set; } = new();

    /// <summary>
    /// Data sources used in the analysis
    /// </summary>
    [JsonPropertyName("sources")]
    public List<DataSource> Sources { get; set; } = new();

    /// <summary>
    /// Observable steps taken by agents
    /// </summary>
    [JsonPropertyName("agentSteps")]
    public List<AgentStepSummary> AgentSteps { get; set; } = new();

    /// <summary>
    /// Timestamp when the analysis was generated
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class Insight
{
    [JsonPropertyName("category")]
    public required string Category { get; set; }

    [JsonPropertyName("insight")]
    public required string Content { get; set; }

    [JsonPropertyName("importance")]
    public string Importance { get; set; } = "medium";
}

public class DataSource
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("dataPoints")]
    public List<string> DataPoints { get; set; } = new();

    [JsonPropertyName("articleCount")]
    public int? ArticleCount { get; set; }
}

public class AgentStepSummary
{
    [JsonPropertyName("agent")]
    public required string Agent { get; set; }

    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
