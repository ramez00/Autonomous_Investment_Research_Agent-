# A.I.R.A. - Technical Code Documentation

**Version**: 1.0.0  
**Framework**: .NET 10  
**Last Updated**: February 5, 2026

---

## Table of Contents

1. [Solution Architecture](#solution-architecture)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [Agent System](#agent-system)
5. [Data Models](#data-models)
6. [External Tools & APIs](#external-tools--apis)
7. [Infrastructure Layer](#infrastructure-layer)
8. [API Layer](#api-layer)
9. [Configuration & Security](#configuration--security)
10. [Testing Strategy](#testing-strategy)
11. [Code Quality & Patterns](#code-quality--patterns)

---

## Solution Architecture

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      AIRA.Api (Web API)                      │
│  - REST API Controllers                                      │
│  - Dependency Injection Configuration                        │
│  - Middleware & Logging                                      │
└────────────────────┬─────────────────────────────────────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
┌──────────┐  ┌──────────┐  ┌────────────────┐
│AIRA.Core │  │  AIRA.   │  │     AIRA.      │
│          │  │ Agents   │  │Infrastructure  │
│- Models  │  │- Agents  │  │- Persistence   │
│- Services│  │- Tools   │  │- Background    │
│- Interfaces│ │- Orch.  │  │  Services      │
└──────────┘  └──────────┘  └────────────────┘
```

### Technology Stack

- **Framework**: .NET 10
- **Web**: ASP.NET Core
- **AI/LLM**: 
  - AutoGen 0.2.3 (Multi-agent orchestration)
  - OpenAI SDK (GPT-4 integration)
- **Database**: 
  - Entity Framework Core 10.0.2
  - SQLite (for development/demo)
- **Logging**: Serilog 10.0.0
- **Resilience**: Polly 8.6.5
- **Configuration**: Microsoft.Extensions.Configuration with User Secrets

---

## Project Structure

### AIRA.Api (Web API Layer)

**Purpose**: Entry point, REST API endpoints, service configuration

**Files**:
- `Program.cs`: Application bootstrap, DI container setup
- `Controllers/AnalysisController.cs`: REST endpoints for analysis requests
- `appsettings.json`: Application configuration
- `Properties/launchSettings.json`: Development environment settings

**Key Dependencies**:
```xml
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
<ProjectReference Include="..\AIRA.Core\AIRA.Core.csproj" />
<ProjectReference Include="..\AIRA.Agents\AIRA.Agents.csproj" />
<ProjectReference Include="..\AIRA.Infrastructure\AIRA.Infrastructure.csproj" />
```

### AIRA.Core (Domain Layer)

**Purpose**: Core business models, interfaces, and domain services

**Structure**:
```
AIRA.Core/
├── Models/
│   ├── AnalysisJob.cs          # Job entity with status tracking
│   ├── AnalysisRequest.cs      # Input DTO
│   ├── AnalysisResult.cs       # Output DTO with insights
│   └── AgentStep.cs            # Observable agent step
├── Interfaces/
│   ├── IJobService.cs          # Job management contract
│   ├── IJobRepository.cs       # Data access contract
│   └── IResearchOrchestrator.cs # Agent orchestration contract
└── Services/
    └── JobService.cs           # Job lifecycle management
```

**Key Features**:
- Pure domain logic, no external dependencies
- Clean interfaces for dependency injection
- Rich domain models with business rules

### AIRA.Agents (Agent System)

**Purpose**: Multi-agent research system, external API tools

**Structure**:
```
AIRA.Agents/
├── Agents/
│   ├── BaseResearchAgent.cs      # Base class for all agents
│   ├── PlannerAgent.cs           # Research planning
│   ├── FinancialDataAgent.cs     # Financial data gathering
│   ├── NewsAnalystAgent.cs       # News sentiment analysis
│   └── SynthesizerAgent.cs       # Final synthesis & thesis
├── Orchestration/
│   └── ResearchOrchestrator.cs   # Multi-agent coordination
└── Tools/
    ├── IFinancialDataTool.cs     # Financial data interface
    ├── INewsTool.cs              # News data interface
    ├── AlphaVantageTool.cs       # Alpha Vantage API
    ├── YahooFinanceTool.cs       # Yahoo Finance API
    ├── NewsApiTool.cs            # News API
    ├── SafeJsonParser.cs         # Robust JSON parsing
    ├── ValidationHelper.cs       # Input validation & sanitization
    ├── Models/                   # Tool data models
    │   ├── FinancialData.cs
    │   └── NewsArticle.cs
    ├── Configuration/
    │   └── SecretsConfiguration.cs
    └── Exceptions/
        └── ToolException.cs      # Custom exceptions
```

**Key Dependencies**:
```xml
<PackageReference Include="AutoGen.OpenAI" Version="0.2.3" />
<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="6.0.1" />
```

### AIRA.Infrastructure (Infrastructure Layer)

**Purpose**: Data persistence, background job processing

**Structure**:
```
AIRA.Infrastructure/
├── Persistence/
│   ├── JobDbContext.cs          # EF Core DbContext
│   ├── JobRepository.cs         # Repository implementation
│   ├── JobEntity.cs             # Database entity
│   └── AgentStepEntity.cs       # Step tracking entity
└── BackgroundServices/
    └── JobProcessorService.cs   # Background job queue processor
```

**Key Dependencies**:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.2" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.2" />
<PackageReference Include="Polly" Version="8.6.5" />
```

---

## Core Components

### 1. Job Service (`JobService.cs`)

**Responsibilities**:
- Create and manage analysis jobs
- Track job lifecycle (Pending → Running → Completed/Failed)
- Record agent steps for observability
- Serialize/deserialize analysis results

**Key Methods**:
```csharp
Task<AnalysisJob> CreateJobAsync(AnalysisRequest request)
Task<AnalysisJob?> GetJobAsync(Guid jobId)
Task AddStepAsync(Guid jobId, AgentStep step)
Task MarkJobStartedAsync(Guid jobId)
Task MarkJobCompletedAsync(Guid jobId, AnalysisResult result)
Task MarkJobFailedAsync(Guid jobId, string errorMessage)
```

**Job States**:
```csharp
public enum JobStatus
{
    Pending,    // Job created, waiting for processing
    Running,    // Currently being processed
    Completed,  // Successfully finished
    Failed      // Error during processing
}
```

### 2. Research Orchestrator (`ResearchOrchestrator.cs`)

**Responsibilities**:
- Coordinate multi-agent workflow
- Execute agents in proper sequence
- Handle parallel data gathering
- Aggregate results from all agents

**Workflow**:
```csharp
public async Task<AnalysisResult> ExecuteResearchAsync(
    AnalysisJob job,
    Func<AgentStep, Task>? onStepCompleted = null,
    CancellationToken cancellationToken = default)
{
    // Phase 1: Planning
    var plan = await _plannerAgent.CreateResearchPlanAsync(...);
    
    // Phase 2: Parallel Data Gathering
    var financialTask = _financialAgent.GatherFinancialDataAsync(...);
    var newsTask = _newsAgent.AnalyzeNewsAsync(...);
    await Task.WhenAll(financialTask, newsTask);
    
    // Phase 3: Synthesis
    var result = await _synthesizerAgent.SynthesizeAsync(...);
    
    return result;
}
```

### 3. Background Job Processor (`JobProcessorService.cs`)

**Responsibilities**:
- Poll for pending jobs from database
- Process jobs asynchronously via Channel
- Invoke orchestrator for each job
- Handle errors and update job status

**Architecture**:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Two parallel tasks:
    // 1. Poll database every 5 seconds for pending jobs
    var pollingTask = PollPendingJobsAsync(stoppingToken);
    
    // 2. Process jobs from queue
    var processingTask = ProcessQueueAsync(stoppingToken);
    
    await Task.WhenAll(pollingTask, processingTask);
}
```

**Queue Implementation**:
- Uses `System.Threading.Channels` for in-memory queue
- Configured as unbounded channel
- Thread-safe, high-performance

---

## Agent System

### Base Research Agent (`BaseResearchAgent.cs`)

**Purpose**: Shared functionality for all agents

**Features**:
- Step recording and callback mechanism
- Consistent logging
- Thread-safe step counter

**Usage Pattern**:
```csharp
public abstract class BaseResearchAgent
{
    protected readonly ILogger Logger;
    public abstract string AgentName { get; }
    
    public void SetStepCallback(Func<AgentStep, Task>? callback, int startingStep = 0);
    
    protected async Task RecordStepAsync(
        string action,
        string? details = null,
        bool isSuccess = true,
        string? errorMessage = null,
        TimeSpan? duration = null);
}
```

### 1. Planner Agent (`PlannerAgent.cs`)

**Purpose**: Create research strategy and plan

**Input**:
- Company name
- Stock symbol
- Analysis depth (quick/standard/deep)

**Output**: `ResearchPlan` object containing:
```csharp
public class ResearchPlan
{
    public string CompanyName { get; set; }
    public string Symbol { get; set; }
    public string AnalysisDepth { get; set; }
    public List<string> FocusAreas { get; set; }
    public List<string> FinancialMetrics { get; set; }
    public List<string> NewsTopics { get; set; }
    public int TimeframeMonths { get; set; }
    public List<string> RiskFactors { get; set; }
}
```

**LLM Integration**:
- Uses GPT-4 to create tailored research plan
- Sanitizes inputs to prevent prompt injection
- Falls back to default plan if LLM fails
- Parses JSON response safely with `SafeJsonParser`

**Security Features**:
- Input sanitization via `ValidationHelper.SanitizeInput()`
- Limits input lengths (company name: 200 chars, symbol: 10 chars)
- Validates and clamps timeframe (6-60 months)

### 2. Financial Data Agent (`FinancialDataAgent.cs`)

**Purpose**: Gather and analyze financial data from multiple sources

**Data Collection Strategy**:
```csharp
// Parallel execution with concurrency limit
var semaphore = new SemaphoreSlim(3); // Max 3 concurrent API calls

foreach (var tool in _tools)
{
    tasks.Add(Task.Run(async () =>
    {
        await semaphore.WaitAsync();
        try
        {
            // Get quote, fundamentals, historical prices
            var quote = await tool.GetQuoteAsync(symbol);
            var fundamentals = await tool.GetFundamentalsAsync(symbol);
            var prices = await tool.GetHistoricalPricesAsync(symbol, 30);
            
            // Merge data with thread-safe locking
            lock (analysis.Data)
            {
                MergeFinancialData(analysis.Data, quote);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }));
}

await Task.WhenAll(tasks);
```

**Data Merging Logic**:
- Prefers non-null values
- Combines data from multiple sources
- Tracks data sources for transparency

**Calculated Metrics**:
- Market cap formatting (K/M/B/T)
- Price position in 52-week range
- Trend analysis (Strong Uptrend/Uptrend/Neutral/Downtrend/Strong Downtrend)

**Output**: `FinancialAnalysis` object with:
- Merged financial data
- Historical prices (limited to 1000 max)
- Calculated metrics dictionary
- Trend assessment
- List of data sources used

### 3. News Analyst Agent (`NewsAnalystAgent.cs`)

**Purpose**: Gather and analyze news sentiment

**Data Collection**:
- Parallel fetching from multiple news sources
- Concurrency limit: 3 simultaneous requests
- Maximum total articles: 100 (across all sources)
- Time-weighted sentiment calculation

**Sentiment Analysis**:
```csharp
private double CalculateOverallSentiment(List<NewsArticle> articles)
{
    // Weight recent articles more heavily
    foreach (var article in articlesWithSentiment)
    {
        var daysAgo = (now - article.PublishedAt).TotalDays;
        var weight = Math.Max(0.1, 1 - (daysAgo / 30)); // Decay over 30 days
        
        weightedSum += article.SentimentScore * weight;
        weightSum += weight;
    }
    
    return weightedSum / weightSum;
}
```

**Sentiment Labels**:
- Very Positive (> 0.3)
- Positive (> 0.1)
- Neutral (-0.1 to 0.1)
- Negative (< -0.1)
- Very Negative (< -0.3)

**Theme Extraction**:
- Keyword-based pattern matching
- Categories: Earnings, Growth, Product, Market, Management, Regulatory, Acquisition, Dividend, Guidance
- Top 5 themes selected by frequency

**LLM Enhancement** (Optional):
- Summarizes top 10 headlines
- Extracts key events
- Assesses potential stock impact
- Sanitizes headlines before sending to LLM

**Output**: `NewsAnalysis` object with:
- List of articles
- Overall sentiment score (-1 to 1)
- Sentiment label
- Key themes
- Recent headlines
- Optional LLM-generated summary and events

### 4. Synthesizer Agent (`SynthesizerAgent.cs`)

**Purpose**: Generate final investment thesis and recommendations

**Input**:
- Research plan
- Financial analysis
- News analysis
- All agent steps

**Process**:
1. Build comprehensive analysis context
2. Send to GPT-4 with structured prompt
3. Parse JSON response for thesis, signal, confidence, insights
4. Add data sources and agent steps
5. Handle errors with fallback logic

**LLM Prompt Structure**:
```json
{
  "thesis": "2-3 sentence investment thesis",
  "signal": "BULLISH | BEARISH | NEUTRAL",
  "confidence": 0.0-1.0,
  "insights": [
    {
      "category": "financial | sentiment | growth | risk",
      "insight": "specific insight",
      "importance": "high | medium | low"
    }
  ]
}
```

**Fallback Strategy**:
- If LLM fails, use rule-based analysis
- Score calculation based on:
  - Price trend (uptrend/downtrend)
  - Sentiment score (positive/negative)
  - Analyst rating
- Confidence based on:
  - Number of data sources
  - Article count
  - Trend strength
  - Sentiment strength

**Output**: `AnalysisResult` with:
- Investment thesis
- Directional signal
- Confidence score (0-1)
- Categorized insights
- Data sources used
- Agent step summary
- Generation timestamp

---

## Data Models

### Core Models (AIRA.Core/Models)

#### AnalysisJob
```csharp
public class AnalysisJob
{
    public Guid Id { get; set; }
    public string CompanySymbol { get; set; }
    public string CompanyName { get; set; }
    public string AnalysisDepth { get; set; } = "standard";
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultJson { get; set; }
    public List<AgentStep> Steps { get; set; } = new();
}
```

#### AnalysisRequest
```csharp
public class AnalysisRequest
{
    public string CompanySymbol { get; set; }  // e.g., "AAPL"
    public string CompanyName { get; set; }    // e.g., "Apple Inc."
    public string AnalysisDepth { get; set; } = "standard"; // quick/standard/deep
}
```

#### AnalysisResult
```csharp
public class AnalysisResult
{
    public string Company { get; set; }
    public string Thesis { get; set; }
    public string Signal { get; set; }  // BULLISH, BEARISH, NEUTRAL
    public double Confidence { get; set; }
    public List<Insight> Insights { get; set; }
    public List<DataSource> Sources { get; set; }
    public List<AgentStepSummary> AgentSteps { get; set; }
    public DateTime GeneratedAt { get; set; }
}
```

#### AgentStep
```csharp
public class AgentStep
{
    public int StepNumber { get; set; }
    public string AgentName { get; set; }
    public string Action { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Tool Models (AIRA.Agents/Tools/Models)

#### FinancialData
```csharp
public class FinancialData
{
    // Price Data
    public decimal? CurrentPrice { get; set; }
    public decimal? PreviousClose { get; set; }
    public decimal? Open { get; set; }
    public decimal? DayHigh { get; set; }
    public decimal? DayLow { get; set; }
    public decimal? Week52High { get; set; }
    public decimal? Week52Low { get; set; }
    public decimal? PriceChange { get; set; }
    public decimal? PriceChangePercent { get; set; }
    
    // Volume
    public long? Volume { get; set; }
    public long? AverageVolume { get; set; }
    
    // Fundamentals
    public decimal? MarketCap { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? ForwardPe { get; set; }
    public decimal? Eps { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? Beta { get; set; }
    
    // Financials
    public decimal? Revenue { get; set; }
    public decimal? RevenueGrowth { get; set; }
    public decimal? GrossProfit { get; set; }
    public decimal? GrossProfitMargin { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? NetProfitMargin { get; set; }
    public decimal? OperatingIncome { get; set; }
    public decimal? Ebitda { get; set; }
    
    // Balance Sheet
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? TotalEquity { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? Cash { get; set; }
    public decimal? DebtToEquity { get; set; }
    public decimal? CurrentRatio { get; set; }
    
    // Analyst Data
    public string? AnalystRating { get; set; }
    public decimal? TargetPrice { get; set; }
    public int? AnalystCount { get; set; }
    
    public DateTime RetrievedAt { get; set; }
    public List<string> DataSources { get; set; }
}
```

#### NewsArticle
```csharp
public class NewsArticle
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string Source { get; set; }
    public string? Author { get; set; }
    public string Url { get; set; }
    public DateTime PublishedAt { get; set; }
    public double? SentimentScore { get; set; }  // -1 to 1
    public double? RelevanceScore { get; set; }  // 0 to 1
}
```

---

## External Tools & APIs

### Tool Interface Pattern

**IFinancialDataTool**:
```csharp
public interface IFinancialDataTool
{
    string Name { get; }
    Task<FinancialData?> GetQuoteAsync(string symbol, CancellationToken ct);
    Task<FinancialData?> GetFundamentalsAsync(string symbol, CancellationToken ct);
    Task<List<HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days, CancellationToken ct);
}
```

**INewsTool**:
```csharp
public interface INewsTool
{
    string Name { get; }
    Task<NewsSearchResult> SearchNewsAsync(
        string companyName,
        string symbol,
        int maxArticles,
        int daysBack,
        CancellationToken ct);
}
```

### 1. Alpha Vantage Tool (`AlphaVantageTool.cs`)

**API Endpoints Used**:
- `GLOBAL_QUOTE`: Real-time quote data
- `OVERVIEW`: Company fundamentals
- `TIME_SERIES_DAILY`: Historical prices

**Configuration**:
```json
{
  "AlphaVantage": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://www.alphavantage.co/query",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  }
}
```

**Security Features**:
- HTTPS enforcement (validates BaseUrl)
- Symbol validation before API calls
- API key validation on construction
- Timeout configuration

**Error Handling**:
- `ApiDataException`: Invalid response format
- `ApiTimeoutException`: Request timeout
- Graceful fallback on missing data

**Data Parsing**:
- Robust JSON parsing with null checks
- Handles "None", "-" as null values
- Percentage parsing (removes '%' character)
- Large number parsing for market cap

### 2. Yahoo Finance Tool (`YahooFinanceTool.cs`)

**Endpoints**:
- Quote data
- Company fundamentals
- Historical prices

**Features**:
- Similar structure to AlphaVantageTool
- Different data schema handling
- Complementary data coverage

### 3. News API Tool (`NewsApiTool.cs`)

**API**: NewsAPI.org

**Configuration**:
```json
{
  "NewsApi": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://newsapi.org/v2",
    "TimeoutSeconds": 30
  }
}
```

**Search Strategy**:
- Query combines company name and symbol
- Sort by relevancy and date
- Language filter: English
- Configurable article count and time range

**Sentiment Analysis**:
- Basic keyword-based sentiment scoring
- Positive keywords: ["growth", "surge", "beat", "profit", "gain", ...]
- Negative keywords: ["loss", "decline", "miss", "warning", "lawsuit", ...]
- Score: (positive_count - negative_count) / total_words

**Data Quality**:
- Filters out removed/deleted articles
- Validates required fields (title, URL)
- Limits article count to prevent memory issues

### Utility Classes

#### SafeJsonParser (`SafeJsonParser.cs`)

**Purpose**: Robust JSON parsing from LLM responses

**Features**:
- Extracts JSON from markdown code blocks
- Handles multiple JSON objects
- Provides detailed error messages
- Generic type support

**Usage**:
```csharp
var result = SafeJsonParser.ParseJsonFromText<MyType>(llmResponse);
```

**Parsing Strategy**:
1. Try direct deserialization
2. Look for markdown code blocks (```json...```)
3. Search for JSON object patterns ({...})
4. Extract and deserialize

#### ValidationHelper (`ValidationHelper.cs`)

**Purpose**: Input validation and sanitization

**Methods**:
```csharp
// Stock symbol validation (1-10 chars, alphanumeric + dots/dashes)
bool IsValidSymbol(string symbol)

// URL validation (HTTPS only)
bool IsHttpsUrl(string url)

// Days back validation (1 to maxDays)
int ValidateDaysBack(int days, int maxDays)

// Input sanitization (removes control chars, limits length)
string SanitizeInput(string input, int maxLength)
```

**Security Features**:
- Prevents control character injection
- Length limiting
- Pattern validation
- HTTPS enforcement

### Exception Hierarchy

```
Exception
└── ToolException (base for all tool errors)
    ├── ApiAuthenticationException (AUTH_ERROR)
    ├── ApiRateLimitException (RATE_LIMIT)
    │   └── RetryAfterSeconds property
    ├── ApiDataException (INVALID_DATA)
    └── ApiTimeoutException (TIMEOUT)

ValidationException (separate hierarchy)
```

**Usage Pattern**:
```csharp
try
{
    var data = await tool.GetQuoteAsync(symbol);
}
catch (ToolException ex)
{
    Logger.LogWarning("Tool error: {ErrorCode}", ex.ErrorCode);
    await RecordStepAsync($"Failed: {ex.ErrorCode}", isSuccess: false);
}
```

---

## Infrastructure Layer

### Database Schema

**JobEntity**:
```sql
CREATE TABLE Jobs (
    Id GUID PRIMARY KEY,
    CompanySymbol NVARCHAR(20) NOT NULL,
    CompanyName NVARCHAR(200) NOT NULL,
    AnalysisDepth NVARCHAR(50),
    Status INTEGER NOT NULL,
    CreatedAt DATETIME NOT NULL,
    StartedAt DATETIME NULL,
    CompletedAt DATETIME NULL,
    ErrorMessage TEXT NULL,
    ResultJson TEXT NULL
);
```

**AgentStepEntity**:
```sql
CREATE TABLE AgentSteps (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    JobId GUID NOT NULL,
    StepNumber INTEGER NOT NULL,
    AgentName NVARCHAR(100) NOT NULL,
    Action NVARCHAR(500) NOT NULL,
    Details TEXT NULL,
    Timestamp DATETIME NOT NULL,
    DurationMs BIGINT NULL,
    IsSuccess BOOLEAN NOT NULL,
    ErrorMessage TEXT NULL,
    FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE CASCADE
);
```

### JobRepository (`JobRepository.cs`)

**Implementation**:
```csharp
public class JobRepository : IJobRepository
{
    private readonly JobDbContext _context;
    
    public async Task<AnalysisJob> CreateAsync(AnalysisJob job)
    {
        var entity = MapToEntity(job);
        _context.Jobs.Add(entity);
        await _context.SaveChangesAsync();
        return job;
    }
    
    public async Task<AnalysisJob?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Jobs
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == id);
        
        return entity == null ? null : MapToModel(entity);
    }
    
    public async Task<IEnumerable<AnalysisJob>> GetPendingJobsAsync()
    {
        var entities = await _context.Jobs
            .Include(j => j.Steps)
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();
        
        return entities.Select(MapToModel);
    }
}
```

**Mapping**:
- Converts between domain models and EF entities
- Preserves step ordering
- Handles nullable properties

### Background Job Processing

**JobProcessorService** (Hosted Service):

**Architecture**:
```
┌─────────────────────────────────────────────┐
│      JobProcessorService (BackgroundService) │
│                                              │
│  ┌──────────────────┐  ┌─────────────────┐  │
│  │  Polling Task    │  │ Processing Task │  │
│  │                  │  │                 │  │
│  │ Every 5 seconds: │  │ Read from       │  │
│  │ - Query pending  │  │ channel         │  │
│  │   jobs from DB   │  │ - Invoke        │  │
│  │ - Write to       │  │   orchestrator  │  │
│  │   channel        │  │ - Update status │  │
│  └──────────────────┘  └─────────────────┘  │
│           │                     │            │
│           └────────┬────────────┘            │
│                    ▼                         │
│        Channel<Guid> (job queue)             │
└─────────────────────────────────────────────┘
```

**Channel Configuration**:
```csharp
builder.Services.AddSingleton(Channel.CreateUnbounded<Guid>(
    new UnboundedChannelOptions
    {
        SingleReader = false,  // Multiple workers possible
        SingleWriter = false   // Multiple sources can enqueue
    }
));
```

**Processing Logic**:
```csharp
private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
{
    using var scope = _serviceProvider.CreateScope();
    var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
    var orchestrator = scope.ServiceProvider.GetRequiredService<IResearchOrchestrator>();
    
    var job = await jobService.GetJobAsync(jobId);
    
    // Skip if already processed
    if (job.Status != JobStatus.Pending) return;
    
    await jobService.MarkJobStartedAsync(jobId);
    
    try
    {
        var result = await orchestrator.ExecuteResearchAsync(
            job,
            async step => await jobService.AddStepAsync(jobId, step),
            ct);
        
        await jobService.MarkJobCompletedAsync(jobId, result);
    }
    catch (Exception ex)
    {
        await jobService.MarkJobFailedAsync(jobId, ex.Message);
    }
}
```

**Benefits**:
- Decouples API response from long-running analysis
- Allows horizontal scaling (multiple processors)
- Provides retry and error handling
- Observable with step-by-step tracking

---

## API Layer

### Program.cs (Application Bootstrap)

**Service Registration**:
```csharp
// Logging
builder.Host.UseSerilog();

// Configuration options
builder.Services.Configure<AlphaVantageOptions>(
    builder.Configuration.GetSection("AlphaVantage"));
builder.Services.Configure<NewsApiOptions>(
    builder.Configuration.GetSection("NewsApi"));

// Database
builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseSqlite(connectionString));

// Core services
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();

// OpenAI client
var openAiClient = new OpenAIClient(apiKey);
builder.Services.AddSingleton(openAiClient.GetChatClient(model));

// HTTP clients with configuration
builder.Services.AddHttpClient<AlphaVantageTool>();
builder.Services.AddHttpClient<YahooFinanceTool>();
builder.Services.AddHttpClient<NewsApiTool>();

// Data tools (multiple implementations)
builder.Services.AddScoped<IFinancialDataTool, AlphaVantageTool>();
builder.Services.AddScoped<IFinancialDataTool, YahooFinanceTool>();
builder.Services.AddScoped<INewsTool, NewsApiTool>();

// Agents
builder.Services.AddScoped<PlannerAgent>();
builder.Services.AddScoped<FinancialDataAgent>();
builder.Services.AddScoped<NewsAnalystAgent>();
builder.Services.AddScoped<SynthesizerAgent>();

// Orchestrator
builder.Services.AddScoped<IResearchOrchestrator, ResearchOrchestrator>();

// Background processing
builder.Services.AddSingleton(Channel.CreateUnbounded<Guid>(...));
builder.Services.AddHostedService<JobProcessorService>();
```

**Middleware Pipeline**:
```csharp
app.UseSerilogRequestLogging();
app.UseDeveloperExceptionPage(); // Development only
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
```

**Built-in Endpoints**:
```csharp
// Health check
app.MapGet("/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
});

// API info
app.MapGet("/", () => new
{
    name = "A.I.R.A. - Autonomous Investment Research Agent",
    version = "1.0.0",
    endpoints = new { ... }
});
```

### AnalysisController (`AnalysisController.cs`)

**Endpoints**:

#### POST /api/analysis
```csharp
[HttpPost]
public async Task<ActionResult<JobSubmissionResponse>> SubmitAnalysis(
    [FromBody] AnalysisRequest request)
{
    // Validation
    if (string.IsNullOrWhiteSpace(request.CompanySymbol))
        return BadRequest(new { error = "CompanySymbol is required" });
    
    if (string.IsNullOrWhiteSpace(request.CompanyName))
        return BadRequest(new { error = "CompanyName is required" });
    
    // Create job
    var job = await _jobService.CreateJobAsync(request);
    
    // Return 202 Accepted with status URL
    return Accepted(new JobSubmissionResponse
    {
        JobId = job.Id,
        Status = job.Status.ToString(),
        Message = $"Analysis job created for {request.CompanyName}",
        StatusUrl = $"/api/analysis/{job.Id}"
    });
}
```

#### GET /api/analysis/{jobId}
```csharp
[HttpGet("{jobId:guid}")]
public async Task<ActionResult<JobStatusResponse>> GetJobStatus(Guid jobId)
{
    var job = await _jobService.GetJobAsync(jobId);
    
    if (job == null)
        return NotFound(new { error = $"Job {jobId} not found" });
    
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
```

#### GET /api/analysis/{jobId}/steps
```csharp
[HttpGet("{jobId:guid}/steps")]
public async Task<ActionResult<IEnumerable<AgentStep>>> GetJobSteps(Guid jobId)
{
    var job = await _jobService.GetJobAsync(jobId);
    
    if (job == null)
        return NotFound(new { error = $"Job {jobId} not found" });
    
    return Ok(job.Steps);
}
```

**Response Models**:
```csharp
public class JobSubmissionResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public string StatusUrl { get; set; }
}

public class JobStatusResponse
{
    public Guid JobId { get; set; }
    public string CompanySymbol { get; set; }
    public string CompanyName { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int StepsCompleted { get; set; }
    public AnalysisResult? Result { get; set; }
}
```

---

## Configuration & Security

### Configuration Sources (Priority Order)

1. **Command-line arguments** (highest priority)
2. **Environment variables** (e.g., `OpenAI__ApiKey`)
3. **User Secrets** (development only)
4. **appsettings.{Environment}.json**
5. **appsettings.json** (lowest priority)

### User Secrets (Development)

**Setup**:
```bash
# Initialize user secrets
dotnet user-secrets init --project src/AIRA.Agents

# Add API keys
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key"
dotnet user-secrets set "AlphaVantage:ApiKey" "your-key"
dotnet user-secrets set "NewsApi:ApiKey" "your-key"
```

**File Location**:
- Windows: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json`

**secrets.json Example**:
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key",
    "Model": "gpt-4"
  },
  "AlphaVantage": {
    "ApiKey": "your-alphavantage-key"
  },
  "NewsApi": {
    "ApiKey": "your-newsapi-key"
  }
}
```

### Environment Variables (Production)

**Windows (PowerShell)**:
```powershell
$env:OpenAI__ApiKey="sk-your-key"
$env:AlphaVantage__ApiKey="your-key"
$env:NewsApi__ApiKey="your-key"
```

**Linux/macOS (Bash)**:
```bash
export OpenAI__ApiKey=sk-your-key
export AlphaVantage__ApiKey=your-key
export NewsApi__ApiKey=your-key
```

**Docker**:
```dockerfile
ENV OpenAI__ApiKey=sk-your-key
ENV AlphaVantage__ApiKey=your-key
ENV NewsApi__ApiKey=your-key
```

### Security Best Practices

**Implemented**:
- ✅ User Secrets for development
- ✅ Environment variable support
- ✅ No secrets in source control
- ✅ `.gitignore` configured
- ✅ `.env.example` template provided
- ✅ HTTPS enforcement for external APIs
- ✅ Input validation and sanitization
- ✅ SQL injection prevention (EF Core parameterization)
- ✅ Prompt injection prevention (input sanitization)

**Recommended for Production**:
- Azure Key Vault integration
- Managed Identity authentication
- API key rotation strategy
- Rate limiting middleware
- Request throttling
- Authentication & authorization
- CORS configuration
- API versioning

---

## Testing Strategy

### Test Project: AIRA.Tests

**Structure**:
```
AIRA.Tests/
├── Agents/
│   └── (Agent unit tests)
├── Models/
│   └── (Model validation tests)
├── Services/
│   └── JobServiceTests.cs
└── Tools/
    └── SentimentAnalysisTests.cs
```

**Testing Framework**:
- xUnit
- Moq (mocking)
- FluentAssertions (assertions)

**Example Test** (JobServiceTests.cs):
```csharp
[Fact]
public async Task CreateJobAsync_ShouldCreatePendingJob()
{
    // Arrange
    var mockRepo = new Mock<IJobRepository>();
    mockRepo.Setup(r => r.CreateAsync(It.IsAny<AnalysisJob>()))
        .ReturnsAsync((AnalysisJob j) => j);
    
    var service = new JobService(mockRepo.Object);
    
    var request = new AnalysisRequest
    {
        CompanySymbol = "AAPL",
        CompanyName = "Apple Inc.",
        AnalysisDepth = "standard"
    };
    
    // Act
    var job = await service.CreateJobAsync(request);
    
    // Assert
    Assert.NotEqual(Guid.Empty, job.Id);
    Assert.Equal("AAPL", job.CompanySymbol);
    Assert.Equal(JobStatus.Pending, job.Status);
    mockRepo.Verify(r => r.CreateAsync(It.IsAny<AnalysisJob>()), Times.Once);
}
```

**Test Categories**:
1. **Unit Tests**: Individual component testing
2. **Integration Tests**: Multi-component interaction
3. **API Tests**: End-to-end endpoint testing
4. **Tool Tests**: External API mocking

**Run Tests**:
```bash
dotnet test
```

---

## Code Quality & Patterns

### Design Patterns Used

#### 1. Repository Pattern
- Abstracts data access
- `IJobRepository` interface with `JobRepository` implementation
- Enables easy testing with mocking

#### 2. Dependency Injection
- Constructor injection throughout
- Interface-based dependencies
- Scoped, Singleton, and Transient lifetimes

#### 3. Strategy Pattern
- Multiple implementations of `IFinancialDataTool`
- Multiple implementations of `INewsTool`
- Selected at runtime via DI

#### 4. Template Method Pattern
- `BaseResearchAgent` defines common workflow
- Derived agents implement specific behavior

#### 5. Observer Pattern
- Agent step callbacks
- Observable job progress

#### 6. Chain of Responsibility
- Multi-agent orchestration
- Sequential processing with shared context

### SOLID Principles

**Single Responsibility**:
- Each agent has one responsibility
- Separate concerns: API, Core, Agents, Infrastructure

**Open/Closed**:
- Open for extension (new tools, new agents)
- Closed for modification (interfaces stable)

**Liskov Substitution**:
- Any `IFinancialDataTool` can replace another
- Any `INewsTool` can replace another

**Interface Segregation**:
- Focused interfaces (IJobService, IJobRepository)
- No fat interfaces with unused methods

**Dependency Inversion**:
- Depend on abstractions (interfaces)
- Not on concrete implementations

### Code Style

**Naming Conventions**:
- PascalCase for classes, methods, properties
- camelCase for local variables, parameters
- `_camelCase` for private fields
- `IInterface` for interfaces

**Async/Await**:
- All I/O operations are async
- Proper cancellation token propagation
- `ConfigureAwait(false)` not used (ASP.NET Core context)

**Nullable Reference Types**:
- Enabled project-wide (`<Nullable>enable</Nullable>`)
- Explicit nullability annotations
- Null-checking where required

**Logging**:
- Structured logging with Serilog
- Log levels: Debug, Information, Warning, Error
- Context-rich log messages

### Performance Considerations

**Concurrency**:
- Parallel data gathering with semaphore limits
- Thread-safe data merging with locks
- Channel-based queue for background processing

**Memory Management**:
- Limits on article counts (100 max)
- Limits on historical prices (1000 max)
- String length sanitization

**Caching**:
- Not implemented (future enhancement)
- Consider Redis for production

**Database**:
- SQLite for simplicity
- Consider PostgreSQL/SQL Server for production
- Index on JobId, Status, CreatedAt

---

## Development Workflow

### Local Development

1. **Clone repository**
2. **Setup secrets**:
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "your-key" --project src/AIRA.Agents
   ```
3. **Build**:
   ```bash
   dotnet build
   ```
4. **Run**:
   ```bash
   dotnet run --project src/AIRA.Api
   ```
5. **Test**:
   ```bash
   curl -X POST http://localhost:5000/api/analysis \
     -H "Content-Type: application/json" \
     -d '{"companySymbol":"AAPL","companyName":"Apple Inc."}'
   ```

### Debugging

**Visual Studio**:
- Set breakpoints in agents
- Debug through orchestration flow
- Inspect LLM prompts and responses

**Logging**:
- Check console output for agent steps
- Serilog provides detailed structured logs
- Filter by agent name or job ID

### Production Deployment

**Prerequisites**:
1. SQL Server or PostgreSQL database
2. Azure Key Vault for secrets
3. Application Insights for monitoring
4. Load balancer for horizontal scaling

**Configuration Changes**:
- Replace SQLite with production database
- Replace in-memory channel with Redis/RabbitMQ
- Enable authentication/authorization
- Configure CORS
- Add rate limiting
- Enable HTTPS only

---

## API Rate Limits & Costs

### External APIs

**Alpha Vantage** (Free Tier):
- 5 requests per minute
- 500 requests per day

**Yahoo Finance** (Unofficial):
- No official limits
- Use responsibly

**NewsAPI** (Free Tier):
- 100 requests per day
- Dev environment only

**OpenAI GPT-4**:
- Pay-per-token pricing
- ~3-4 API calls per analysis
- Cost: ~$0.01-0.05 per analysis

### Mitigation Strategies

1. **Caching**: Cache financial data (TTL: 15 min)
2. **Rate Limiting**: Implement request throttling
3. **Batch Processing**: Queue jobs during high demand
4. **Fallback**: Use cached/default data when APIs fail
5. **Monitoring**: Track API usage and costs

---

## Troubleshooting

### Common Issues

**1. Missing API Keys**
```
Error: OpenAI API key not configured
Solution: Set user secrets or environment variables
```

**2. Rate Limit Exceeded**
```
Error: RATE_LIMIT from AlphaVantage
Solution: Wait 60 seconds or upgrade API tier
```

**3. LLM Parsing Failure**
```
Error: Failed to parse synthesis JSON
Solution: Check SafeJsonParser logs, fallback used automatically
```

**4. Database Lock**
```
Error: SQLite database locked
Solution: Ensure single writer, or switch to PostgreSQL
```

### Debugging Tips

1. **Enable verbose logging**:
   ```json
   "Serilog": {
     "MinimumLevel": {
       "Default": "Debug"
     }
   }
   ```

2. **Check agent steps**:
   ```bash
   curl http://localhost:5000/api/analysis/{jobId}/steps
   ```

3. **Inspect database**:
   ```bash
   sqlite3 aira.db
   SELECT * FROM Jobs;
   SELECT * FROM AgentSteps ORDER BY Timestamp;
   ```

---

## Future Enhancements

### Short-term
- [ ] Add request caching layer (Redis)
- [ ] Implement API rate limiting middleware
- [ ] Add authentication & authorization
- [ ] Create admin dashboard for monitoring
- [ ] Add webhook notifications on completion

### Medium-term
- [ ] Support for batch analysis (multiple companies)
- [ ] Historical result comparison
- [ ] Confidence scoring based on data quality
- [ ] Multi-LLM provider support (Azure OpenAI, Anthropic)
- [ ] Advanced sentiment analysis (dedicated NLP model)

### Long-term
- [ ] Proactive/scheduled analysis
- [ ] Machine learning for signal prediction
- [ ] Real-time data streaming
- [ ] Portfolio-level analysis
- [ ] Mobile app integration

---

## References

- [AutoGen Documentation](https://microsoft.github.io/autogen/)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Alpha Vantage API](https://www.alphavantage.co/documentation/)
- [NewsAPI Documentation](https://newsapi.org/docs)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

---

**Document Version**: 1.0.0  
**Last Updated**: February 5, 2026  
**Maintained By**: Development Team
