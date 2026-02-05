using System.Text.Json;
using AIRA.Core.Models;

namespace AIRA.Tests.Models;

public class AnalysisResultTests
{
    [Fact]
    public void AnalysisResult_SerializesToRequiredSchema()
    {
        // Arrange
        var result = new AnalysisResult
        {
            Company = "Apple Inc. (AAPL)",
            Thesis = "Strong fundamentals with growth potential",
            Signal = "BULLISH",
            Confidence = 0.85,
            Insights = new List<Insight>
            {
                new() { Category = "financial", Content = "Revenue up 15%", Importance = "high" },
                new() { Category = "sentiment", Content = "Positive news", Importance = "medium" }
            },
            Sources = new List<DataSource>
            {
                new() { Type = "financial", Source = "AlphaVantage", DataPoints = new List<string> { "quote" } },
                new() { Type = "news", Source = "NewsAPI", ArticleCount = 10 }
            },
            GeneratedAt = new DateTime(2026, 2, 4, 10, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert - Check required top-level keys exist
        Assert.True(deserialized.TryGetProperty("company", out _));
        Assert.True(deserialized.TryGetProperty("thesis", out _));
        Assert.True(deserialized.TryGetProperty("signal", out _));
        Assert.True(deserialized.TryGetProperty("insights", out _));
        Assert.True(deserialized.TryGetProperty("sources", out _));
        
        // Verify values
        Assert.Equal("Apple Inc. (AAPL)", deserialized.GetProperty("company").GetString());
        Assert.Equal("BULLISH", deserialized.GetProperty("signal").GetString());
        Assert.Equal(0.85, deserialized.GetProperty("confidence").GetDouble(), 2);
    }

    [Fact]
    public void AnalysisResult_DeserializesFromJson()
    {
        // Arrange
        var json = @"{
            ""company"": ""Microsoft Corporation (MSFT)"",
            ""thesis"": ""Cloud growth driving revenue"",
            ""signal"": ""BULLISH"",
            ""confidence"": 0.78,
            ""insights"": [
                {""category"": ""growth"", ""insight"": ""Azure revenue up 30%"", ""importance"": ""high""}
            ],
            ""sources"": [
                {""type"": ""financial"", ""source"": ""YahooFinance"", ""dataPoints"": [""quote"", ""fundamentals""]}
            ],
            ""generatedAt"": ""2026-02-04T10:00:00Z""
        }";

        // Act
        var result = JsonSerializer.Deserialize<AnalysisResult>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Microsoft Corporation (MSFT)", result.Company);
        Assert.Equal("BULLISH", result.Signal);
        Assert.Equal(0.78, result.Confidence);
        Assert.Single(result.Insights);
        Assert.Equal("growth", result.Insights[0].Category);
    }

    [Theory]
    [InlineData("BULLISH")]
    [InlineData("BEARISH")]
    [InlineData("NEUTRAL")]
    public void AnalysisResult_AcceptsValidSignals(string signal)
    {
        // Arrange & Act
        var result = new AnalysisResult
        {
            Company = "Test Co",
            Thesis = "Test thesis",
            Signal = signal
        };

        // Assert
        Assert.Equal(signal, result.Signal);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void AnalysisResult_AcceptsValidConfidence(double confidence)
    {
        // Arrange & Act
        var result = new AnalysisResult
        {
            Company = "Test Co",
            Thesis = "Test thesis",
            Signal = "NEUTRAL",
            Confidence = confidence
        };

        // Assert
        Assert.Equal(confidence, result.Confidence);
    }
}
