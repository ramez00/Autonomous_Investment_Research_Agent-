namespace AIRA.Tests.Tools;

public class SentimentAnalysisTests
{
    // Test the simple sentiment analysis logic used in NewsApiTool
    
    [Theory]
    [InlineData("Stock surges 10% on strong earnings")]
    [InlineData("Company reports record profit")]
    [InlineData("Growth exceeds expectations")]
    [InlineData("Rally continues as market bulls")]
    public void CalculateSentiment_PositiveHeadlines_ReturnsPositive(string headline)
    {
        // Arrange & Act
        var sentiment = CalculateSimpleSentiment(headline, null);

        // Assert
        Assert.True(sentiment > 0, $"Expected positive sentiment for: {headline}");
    }

    [Theory]
    [InlineData("Stock plunges after earnings miss")]
    [InlineData("Company faces lawsuit over scandal")]
    [InlineData("Layoffs announced amid weak sales")]
    [InlineData("Bearish outlook as competition increases")]
    public void CalculateSentiment_NegativeHeadlines_ReturnsNegative(string headline)
    {
        // Arrange & Act
        var sentiment = CalculateSimpleSentiment(headline, null);

        // Assert
        Assert.True(sentiment < 0, $"Expected negative sentiment for: {headline}");
    }

    [Theory]
    [InlineData("Company announces new office location")]
    [InlineData("CEO speaks at industry conference")]
    [InlineData("Quarterly report due next week")]
    public void CalculateSentiment_NeutralHeadlines_ReturnsNearZero(string headline)
    {
        // Arrange & Act
        var sentiment = CalculateSimpleSentiment(headline, null);

        // Assert
        Assert.True(Math.Abs(sentiment) < 0.5, $"Expected neutral sentiment for: {headline}");
    }

    [Fact]
    public void CalculateSentiment_CombinesHeadlineAndDescription()
    {
        // Arrange
        var title = "Company reports quarterly results";
        var description = "Revenue surged 20% beating analyst expectations with record profits";

        // Act
        var sentiment = CalculateSimpleSentiment(title, description);

        // Assert - Combined text should be positive due to description
        Assert.True(sentiment > 0);
    }

    // Replication of the sentiment logic from NewsApiTool for testing
    private static double CalculateSimpleSentiment(string? title, string? description)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        
        var positiveWords = new[]
        {
            "surge", "soar", "gain", "rise", "jump", "rally", "boom", "growth",
            "profit", "beat", "exceed", "record", "strong", "bullish", "upgrade",
            "buy", "outperform", "positive", "success", "breakthrough", "innovation",
            "expand", "win", "award", "best", "leading", "top", "excellent"
        };

        var negativeWords = new[]
        {
            "drop", "fall", "decline", "crash", "plunge", "loss", "miss", "fail",
            "weak", "bearish", "downgrade", "sell", "underperform", "negative",
            "concern", "risk", "warning", "lawsuit", "investigation", "scandal",
            "layoff", "cut", "worst", "trouble", "problem", "crisis", "debt"
        };

        int positiveCount = positiveWords.Count(w => text.Contains(w));
        int negativeCount = negativeWords.Count(w => text.Contains(w));
        int totalCount = positiveCount + negativeCount;

        if (totalCount == 0)
            return 0;

        return (double)(positiveCount - negativeCount) / totalCount;
    }
}
