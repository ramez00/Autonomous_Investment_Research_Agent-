# A.I.R.A. - Autonomous Investment Research Agent

An AI-powered backend service that analyzes publicly traded companies and produces structured, data-driven investment research reports.

## Architecture Overview

A.I.R.A. is built as a multi-agent system using .NET and Microsoft's AutoGen framework, combining structured financial data with unstructured news analysis to generate investment insights.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ASP.NET Core Web API                            │
│  ┌──────────────────┐  ┌──────────────┐  ┌─────────────────────────┐    │
│  │AnalysisController│  │  JobService  │  │ BackgroundJobProcessor  │    │
│  └────────┬─────────┘  └──────┬───────┘  └───────────┬─────────────┘    │
└───────────┼──────────────────┼───────────────────────┼──────────────────┘
            │                  │                       │
            ▼                  ▼                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      Multi-Agent Orchestration                          │
│  ┌─────────────┐  ┌──────────────────┐  ┌───────────────────┐           │
│  │PlannerAgent │─▶│FinancialDataAgent│  │  NewsAnalystAgent │          │
│  └──────┬──────┘  └────────┬─────────┘  └─────────┬─────────┘           │
│         │                  │      (parallel)      │                     │
│         │                  └──────────┬───────────┘                     │
│         │                             ▼                                 │
│         │                    ┌─────────────────┐                        │
│         └───────────────────▶│SynthesizerAgent │                        │
│                              └────────┬────────┘                        │
└───────────────────────────────────────┼─────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          External Data Sources                          │
│  ┌─────────────┐    ┌──────────────┐    ┌─────────────┐                 │
│  │Alpha Vantage│    │Yahoo Finance │    │   NewsAPI   │                 │
│  │ (Financials)│    │(Stock Prices)│    │   (News)    │                 │
│  └─────────────┘    └──────────────┘    └─────────────┘                 │
└─────────────────────────────────────────────────────────────────────────┘
```

## Agentic Workflow

A.I.R.A. operates as a multi-step autonomous agent with observable intermediate steps:

### Phase 1: Planning
The **PlannerAgent** receives the company analysis request and:
- Decomposes the task into specific research areas
- Identifies key financial metrics to analyze
- Determines relevant news topics to search
- Creates a structured research plan

### Phase 2: Parallel Data Gathering
Two agents work concurrently:

**FinancialDataAgent**:
- Retrieves stock quotes from multiple sources (Alpha Vantage, Yahoo Finance)
- Gathers fundamental data (P/E, EPS, margins, debt ratios)
- Collects historical price data for trend analysis
- Merges data from multiple sources for completeness

**NewsAnalystAgent**:
- Searches for recent company news from NewsAPI
- Performs sentiment analysis on headlines
- Extracts key themes and events
- Uses LLM to summarize news impact

### Phase 3: Synthesis
The **SynthesizerAgent**:
- Combines all financial and news data
- Uses GPT-4 to generate an investment thesis
- Determines a directional signal (BULLISH/BEARISH/NEUTRAL)
- Calculates confidence score
- Produces categorized insights

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/analysis` | POST | Submit a company analysis request |
| `/api/analysis/{jobId}` | GET | Get job status and results |
| `/api/analysis/{jobId}/steps` | GET | Get observable agent steps |
| `/health` | GET | Health check |

### Submit Analysis Request

```bash
POST /api/analysis
Content-Type: application/json

{
  "companySymbol": "AAPL",
  "companyName": "Apple Inc.",
  "analysisDepth": "standard"
}
```

**Response (202 Accepted)**:
```json
{
  "jobId": "a1b2c3d4-...",
  "status": "Pending",
  "message": "Analysis job created for Apple Inc. (AAPL)",
  "statusUrl": "/api/analysis/a1b2c3d4-..."
}
```

### Check Status / Get Results

```bash
GET /api/analysis/{jobId}
```

**Response (200 OK)**:
```json
{
  "jobId": "a1b2c3d4-...",
  "companySymbol": "AAPL",
  "companyName": "Apple Inc.",
  "status": "Completed",
  "createdAt": "2026-02-04T10:00:00Z",
  "startedAt": "2026-02-04T10:00:01Z",
  "completedAt": "2026-02-04T10:00:15Z",
  "stepsCompleted": 8,
  "result": {
    "company": "Apple Inc. (AAPL)",
    "thesis": "Strong buy recommendation based on solid fundamentals...",
    "signal": "BULLISH",
    "confidence": 0.82,
    "insights": [...],
    "sources": [...],
    "agentSteps": [...],
    "generatedAt": "2026-02-04T10:00:15Z"
  }
}
```

## Output Schema

The final analysis conforms to the required schema:

```json
{
  "company": "Apple Inc. (AAPL)",
  "thesis": "Strong buy recommendation based on robust revenue growth, healthy margins, and positive market sentiment following recent product announcements.",
  "signal": "BULLISH",
  "confidence": 0.82,
  "insights": [
    {
      "category": "financial",
      "insight": "Revenue growth of 15% YoY with improving gross margins",
      "importance": "high"
    },
    {
      "category": "sentiment",
      "insight": "Positive news sentiment (0.7) driven by product launches",
      "importance": "medium"
    },
    {
      "category": "risk",
      "insight": "Supply chain concerns in Asia may impact Q4 production",
      "importance": "medium"
    }
  ],
  "sources": [
    {
      "type": "financial",
      "source": "AlphaVantage",
      "dataPoints": ["income_statement", "balance_sheet", "quote"]
    },
    {
      "type": "news",
      "source": "NewsAPI",
      "articleCount": 15
    }
  ],
  "agentSteps": [
    {"agent": "Planner", "action": "Created research plan with 5 focus areas", "timestamp": "..."},
    {"agent": "FinancialData", "action": "Retrieved data from AlphaVantage", "timestamp": "..."},
    {"agent": "NewsAnalyst", "action": "Analyzed 15 news articles", "timestamp": "..."},
    {"agent": "Synthesizer", "action": "Generated investment thesis", "timestamp": "..."}
  ],
  "generatedAt": "2026-02-04T10:00:15Z"
}
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- **LLM Provider** (Choose ONE - see [LLM Setup Guide](LLM_SETUP_GUIDE.md)):
  - **Groq** (RECOMMENDED) - FREE API with 14,400 requests/day ([Get API Key](https://console.groq.com))
  - **Ollama** - FREE local LLM, unlimited usage ([Download](https://ollama.ai))
  - **OpenAI** - PAID service, requires credits ([Get API Key](https://platform.openai.com))
- **Data Provider API Keys** (FREE):
  - Alpha Vantage API key ([Get Free Key](https://www.alphavantage.co/support/#api-key))
  - NewsAPI key ([Get Free Key](https://newsapi.org/register))

### Quick Setup (Recommended: Groq)

**Option 1: Using Groq (FREE - Recommended)**

```bash
# 1. Get free API key from https://console.groq.com
# 2. Run the setup script
.\setup-groq.ps1 gsk_your_api_key_here

# 3. Add your data provider keys to appsettings.json
# See Configuration section below
```

**Option 2: Using Ollama (FREE - Local)**

```bash
# 1. Install Ollama from https://ollama.ai
# 2. Run the setup script (auto-downloads model)
.\setup-ollama.ps1
```

**Option 3: Using OpenAI (PAID)**

See detailed configuration in [LLM Setup Guide](LLM_SETUP_GUIDE.md)

### Configuration

#### Complete appsettings.json Setup

Edit `src/AIRA.Api/appsettings.json`:

```json
{
  "LLM": {
    "Provider": "groq"  // Options: "groq", "ollama", "openai"
  },
  "Groq": {
    "ApiKey": "gsk_your_groq_api_key",
    "Model": "llama-3.3-70b-versatile"
  },
  "AlphaVantage": {
    "ApiKey": "your-alphavantage-key"
  },
  "NewsApi": {
    "ApiKey": "your-newsapi-key"
  }
}
```

#### Using Environment Variables (Alternative)

```bash
# LLM Provider
export LLM__Provider=groq
export Groq__ApiKey=gsk-your-key

# Data Providers
export AlphaVantage__ApiKey=your-key
export NewsApi__ApiKey=your-key
```

#### LLM Provider Comparison

| Provider | Cost | Speed | Setup | Best For |
|----------|------|-------|-------|----------|
| **Groq** | FREE | ⚡ Fast | Easy | Most users (RECOMMENDED) |
| **Ollama** | FREE | Medium | Medium | Privacy, unlimited usage |
| **OpenAI** | PAID | Fast | Easy | Enterprise, highest quality |

📚 **Detailed setup instructions**: See [LLM_SETUP_GUIDE.md](LLM_SETUP_GUIDE.md)

### Running the Service

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/AIRA.Api

# The API will be available at https://localhost:5001 (or http://localhost:5000)
```

### Example Usage

```bash
# Submit an analysis
curl -X POST http://localhost:5000/api/analysis \
  -H "Content-Type: application/json" \
  -d '{"companySymbol": "MSFT", "companyName": "Microsoft Corporation"}'

# Check status (replace {jobId} with actual ID)
curl http://localhost:5000/api/analysis/{jobId}

# View agent steps
curl http://localhost:5000/api/analysis/{jobId}/steps
```

## Project Structure

```
AIIRA-Agent/
├── src/
│   ├── AIRA.Api/                    # Web API entry point
│   │   ├── Controllers/             # API controllers
│   │   └── Program.cs               # Service configuration
│   ├── AIRA.Core/                   # Core domain models
│   │   ├── Models/                  # AnalysisJob, AnalysisResult, etc.
│   │   ├── Interfaces/              # Service contracts
│   │   └── Services/                # Business logic
│   ├── AIRA.Agents/                 # Agent implementations
│   │   ├── Agents/                  # Planner, Financial, News, Synthesizer
│   │   ├── Orchestration/           # Multi-agent coordination
│   │   └── Tools/                   # External API integrations
│   └── AIRA.Infrastructure/         # Data access, background services
│       ├── Persistence/             # SQLite repository
│       └── BackgroundServices/      # Job processor
├── tests/
│   └── AIRA.Tests/                  # Unit tests
├── AIRA.sln
└── README.md
```

## Key Design Decisions

### 1. Multi-Agent Architecture
- **Why**: Separates concerns and allows parallel execution of independent tasks
- **Trade-off**: More complexity vs. better modularity and extensibility

### 2. SQLite for Job Persistence
- **Why**: Simple, file-based, zero configuration needed
- **Trade-off**: Not suitable for high-volume production (swap for PostgreSQL/SQL Server)

### 3. Background Service with Channels
- **Why**: Decouples API response from analysis execution
- **Trade-off**: In-memory queue doesn't survive restarts (add Redis/RabbitMQ for durability)

### 4. Multiple Financial Data Sources
- **Why**: Redundancy and data completeness (different APIs have different coverage)
- **Trade-off**: More API calls, potential rate limit issues

### 5. Simple Keyword-Based Sentiment Analysis
- **Why**: Fast, no additional API costs, works as baseline
- **Trade-off**: Less accurate than dedicated NLP models (can add specialized sentiment API)

## Trade-offs & Limitations

- **Rate Limits**: Free API tiers have strict limits (Alpha Vantage: 5/min, NewsAPI: 100/day)
- **Data Freshness**: Financial data may be delayed 15-20 minutes (use paid APIs for real-time)
- **Single Company**: Current design processes one company at a time
- **No Historical Tracking**: Results not persisted for long-term trend analysis
- **LLM Dependency**: Synthesis quality depends on GPT-4 availability and response quality

## Future Enhancements

- [ ] Proactive/recurring analysis with scheduling
- [ ] Confidence scoring based on data quality
- [ ] Caching layer for repeated queries
- [ ] Support for multiple LLM providers
- [ ] Webhook notifications on completion
- [ ] Batch analysis for multiple companies
- [ ] Historical result comparison

## Running Tests

```bash
dotnet test
```

## License

MIT License
