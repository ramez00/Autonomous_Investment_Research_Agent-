# Quick Setup Script for Groq (FREE LLM Provider)
# This script helps you configure AIIRA to use Groq instead of OpenAI

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AIIRA - Groq Setup (FREE)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if API key is provided
$apiKey = $args[0]
if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "Usage: .\setup-groq.ps1 <your-groq-api-key>" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Steps to get your FREE Groq API key:" -ForegroundColor Green
    Write-Host "1. Visit: https://console.groq.com" -ForegroundColor White
    Write-Host "2. Sign up (no credit card required)" -ForegroundColor White
    Write-Host "3. Create an API key" -ForegroundColor White
    Write-Host "4. Run: .\setup-groq.ps1 gsk_your_api_key_here" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Validate API key format
if (-not $apiKey.StartsWith("gsk_")) {
    Write-Host "Warning: Groq API keys usually start with 'gsk_'" -ForegroundColor Yellow
    Write-Host "Are you sure this is correct? Press Enter to continue or Ctrl+C to cancel..."
    Read-Host
}

# Update appsettings.json
$appsettingsPath = "src\AIRA.Api\appsettings.json"
if (Test-Path $appsettingsPath) {
    Write-Host "Updating $appsettingsPath..." -ForegroundColor Yellow
    
    $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    $json.LLM.Provider = "groq"
    $json.Groq.ApiKey = $apiKey
    
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
Write-Host "Your AIIRA agent is now configured to use Groq (FREE)" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet run --project src\AIRA.Api" -ForegroundColor White
Write-Host "2. Submit an analysis request" -ForegroundColor White
Write-Host "3. Enjoy FREE AI-powered investment research!" -ForegroundColor White
Write-Host ""
Write-Host "Groq Free Tier Limits:" -ForegroundColor Cyan
Write-Host "- 14,400 requests per day" -ForegroundColor White
Write-Host "- 30 requests per minute" -ForegroundColor White
Write-Host "- No credit card required!" -ForegroundColor White
Write-Host ""
