using System.Threading.Channels;
using AIRA.Agents.Agents;
using AIRA.Agents.Orchestration;
using AIRA.Agents.Tools;
using AIRA.Agents.LLM;
using AIRA.Core.Interfaces;
using AIRA.Core.Services;
using AIRA.Infrastructure.BackgroundServices;
using AIRA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if(builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuration options
builder.Services.Configure<AlphaVantageOptions>(
    builder.Configuration.GetSection("AlphaVantage"));
builder.Services.Configure<NewsApiOptions>(
    builder.Configuration.GetSection("NewsApi"));

var alphaVantageApiKey = builder.Configuration["AlphaVantage:ApiKey"];
var newsApiKey = builder.Configuration["NewsApi:ApiKey"];
Log.Information("AlphaVantage API Key configured: {IsConfigured}", !string.IsNullOrWhiteSpace(alphaVantageApiKey));
Log.Information("NewsApi API Key configured: {IsConfigured}", !string.IsNullOrWhiteSpace(newsApiKey));

if (string.IsNullOrWhiteSpace(alphaVantageApiKey))
{
    Log.Warning("AlphaVantage API key is missing! Check your user secrets.");
}

// Database
builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=aira.db"));

// Core services
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();

// LLM Configuration - Support multiple providers
var llmProvider = builder.Configuration["LLM:Provider"]?.ToLowerInvariant() ?? "groq";
Log.Information("LLM Provider: {Provider}", llmProvider);

switch (llmProvider)
{
    case "openai":
        var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAiApiKey))
        {
            builder.Services.AddSingleton<ILlmClient>(sp =>
            {
                var client = new OpenAIClient(openAiApiKey);
                var model = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
                var chatClient = client.GetChatClient(model);
                var logger = sp.GetRequiredService<ILogger<OpenAILlmClient>>();
                return new OpenAILlmClient(chatClient, logger);
            });
            Log.Information("OpenAI LLM client configured with model: {Model}", builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini");
        }
        else
        {
            Log.Warning("OpenAI API key not configured. LLM features will be disabled.");
        }
        break;

    case "groq":
        var groqApiKey = builder.Configuration["Groq:ApiKey"];
        if (!string.IsNullOrEmpty(groqApiKey))
        {
            builder.Services.AddHttpClient<GroqClient>();
            builder.Services.AddScoped<ILlmClient>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GroqClient));
                var logger = sp.GetRequiredService<ILogger<GroqClient>>();
                var model = builder.Configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
                return new GroqClient(httpClient, logger, groqApiKey, model);
            });
            Log.Information("Groq LLM client configured (FREE) with model: {Model}", builder.Configuration["Groq:Model"] ?? "llama-3.3-70b-versatile");
        }
        else
        {
            Log.Warning("Groq API key not configured. Get free API key from https://console.groq.com");
        }
        break;

    case "ollama":
        builder.Services.AddHttpClient<OllamaClient>();
        builder.Services.AddScoped<ILlmClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OllamaClient));
            var logger = sp.GetRequiredService<ILogger<OllamaClient>>();
            var model = builder.Configuration["Ollama:Model"] ?? "llama3.2";
            var baseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            return new OllamaClient(httpClient, logger, model, baseUrl);
        });
        Log.Information("Ollama LLM client configured (FREE, LOCAL) with model: {Model}", builder.Configuration["Ollama:Model"] ?? "llama3.2");
        break;

    default:
        Log.Warning("Unknown LLM provider: {Provider}. Valid options: openai, groq, ollama", llmProvider);
        break;
}

// HTTP clients for external APIs
builder.Services.AddHttpClient<AlphaVantageTool>();
builder.Services.AddHttpClient<YahooFinanceTool>();
builder.Services.AddHttpClient<NewsApiTool>();

// Data tools
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

// Job queue and background processor
builder.Services.AddSingleton(Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
{
    SingleReader = false,
    SingleWriter = false
}));
builder.Services.AddHostedService<JobProcessorService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// API info endpoint
app.MapGet("/", () => new
{
    name = "A.I.R.A. - Autonomous Investment Research Agent",
    version = "1.0.0",
    endpoints = new
    {
        submitAnalysis = "POST /api/analysis",
        getStatus = "GET /api/analysis/{jobId}",
        getSteps = "GET /api/analysis/{jobId}/steps",
        health = "GET /health"
    }
});

Log.Information("A.I.R.A. starting up...");

app.Run();
