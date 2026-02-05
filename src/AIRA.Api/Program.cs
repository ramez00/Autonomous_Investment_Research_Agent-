using System.Threading.Channels;
using AIRA.Agents.Agents;
using AIRA.Agents.Orchestration;
using AIRA.Agents.Tools;
using AIRA.Core.Interfaces;
using AIRA.Core.Services;
using AIRA.Infrastructure.BackgroundServices;
using AIRA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

// Database
builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=aira.db"));

// Core services
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();

// OpenAI client
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
if (!string.IsNullOrEmpty(openAiApiKey))
{
    builder.Services.AddSingleton(sp =>
    {
        var client = new OpenAIClient(openAiApiKey);
        var model = builder.Configuration["OpenAI:Model"] ?? "gpt-4";
        return client.GetChatClient(model);
    });
}
else
{
    Log.Warning("OpenAI API key not configured. LLM features will be limited.");
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
