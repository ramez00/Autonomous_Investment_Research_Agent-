# Quick Setup Script for Ollama (FREE Local LLM)
# This script helps you configure AIIRA to use Ollama

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AIIRA - Ollama Setup (FREE & LOCAL)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Ollama is installed
Write-Host "Checking for Ollama installation..." -ForegroundColor Yellow
$ollamaInstalled = Get-Command ollama -ErrorAction SilentlyContinue

if (-not $ollamaInstalled) {
    Write-Host "❌ Ollama is not installed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Ollama first:" -ForegroundColor Yellow
    Write-Host "1. Visit: https://ollama.ai" -ForegroundColor White
    Write-Host "2. Download and install Ollama for Windows" -ForegroundColor White
    Write-Host "3. Run this script again" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "✓ Ollama is installed" -ForegroundColor Green

# Check if Ollama is running
Write-Host "Checking if Ollama is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -TimeoutSec 2 -ErrorAction Stop
    Write-Host "✓ Ollama is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Ollama is not running" -ForegroundColor Red
    Write-Host "Please start Ollama from the system tray or by running 'ollama serve'" -ForegroundColor Yellow
    exit 1
}

# Check available models
Write-Host ""
Write-Host "Checking for available models..." -ForegroundColor Yellow
$models = ollama list

if ($models -match "llama3.2") {
    Write-Host "✓ llama3.2 model found" -ForegroundColor Green
    $modelName = "llama3.2"
} elseif ($models -match "llama3.1") {
    Write-Host "✓ llama3.1 model found" -ForegroundColor Green
    $modelName = "llama3.1:8b"
} else {
    Write-Host "No suitable model found. Downloading llama3.2..." -ForegroundColor Yellow
    Write-Host "This may take a few minutes (about 2GB download)" -ForegroundColor Cyan
    ollama pull llama3.2
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Model downloaded successfully" -ForegroundColor Green
        $modelName = "llama3.2"
    } else {
        Write-Host "❌ Failed to download model" -ForegroundColor Red
        exit 1
    }
}

# Update appsettings.json
$appsettingsPath = "src\AIRA.Api\appsettings.json"
if (Test-Path $appsettingsPath) {
    Write-Host ""
    Write-Host "Updating $appsettingsPath..." -ForegroundColor Yellow
    
    $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    $json.LLM.Provider = "ollama"
    $json.Ollama.Model = $modelName
    
    $json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
    
    Write-Host "✓ Configuration updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Error: Could not find $appsettingsPath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your AIIRA agent is now configured to use Ollama" -ForegroundColor Green
Write-Host "Model: $modelName" -ForegroundColor Cyan
Write-Host ""
Write-Host "Benefits:" -ForegroundColor Yellow
Write-Host "✓ 100% FREE - No API costs" -ForegroundColor White
Write-Host "✓ Unlimited usage - No rate limits" -ForegroundColor White
Write-Host "✓ Complete privacy - Everything runs locally" -ForegroundColor White
Write-Host "✓ Works offline" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet run --project src\AIRA.Api" -ForegroundColor White
Write-Host "2. Submit an analysis request" -ForegroundColor White
Write-Host "3. Enjoy FREE local AI!" -ForegroundColor White
Write-Host ""
