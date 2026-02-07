# 🚀 Quick Start Guide - AIIRA with FREE LLM

Get your AIIRA investment research agent running in **5 minutes** with **FREE** AI models!

---

## Option 1: Groq (Recommended - Cloud, Free)

### 1️⃣ Get Free API Key
Visit https://console.groq.com and sign up (no credit card needed!)

### 2️⃣ Run Setup Script
```powershell
.\setup-groq.ps1 gsk_your_api_key_here
```

### 3️⃣ Add Data Provider Keys
Edit `src/AIRA.Api/appsettings.json` and add:
- Alpha Vantage API key (get free at: https://www.alphavantage.co/support/#api-key)
- NewsAPI key (get free at: https://newsapi.org/register)

```json
{
  "AlphaVantage": {
    "ApiKey": "YOUR_KEY_HERE"
  },
  "NewsApi": {
    "ApiKey": "YOUR_KEY_HERE"
  }
}
```

### 4️⃣ Run!
```bash
dotnet run --project src/AIRA.Api
```

### 5️⃣ Test
```bash
curl -X POST http://localhost:5000/api/analysis \
  -H "Content-Type: application/json" \
  -d '{"companyName": "Apple Inc", "symbol": "AAPL", "analysisDepth": "quick"}'
```

✅ **Done!** You're now running AI-powered investment research for FREE!

---

## Option 2: Ollama (Local, Free, Unlimited)

### 1️⃣ Install Ollama
Download from: https://ollama.ai

### 2️⃣ Run Setup Script
```powershell
.\setup-ollama.ps1
```
(This will auto-download the AI model)

### 3️⃣ Add Data Provider Keys
Same as Option 1, step 3

### 4️⃣ Run!
```bash
dotnet run --project src/AIRA.Api
```

✅ **Done!** Everything runs locally with no API costs!

---

## 📊 What You Get

After submitting a request, you'll receive:

```json
{
  "company": "Apple Inc. (AAPL)",
  "thesis": "Strong buy recommendation based on robust growth...",
  "signal": "BULLISH",
  "confidence": 0.85,
  "insights": [
    {
      "category": "financial",
      "insight": "Revenue growth of 15% YoY...",
      "importance": "high"
    }
  ]
}
```

---

## 🆘 Need Help?

- **Full documentation**: See [README.md](README.md)
- **Detailed LLM setup**: See [LLM_SETUP_GUIDE.md](LLM_SETUP_GUIDE.md)
- **Migration from OpenAI**: See [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)

---

## 💡 Why Use Groq?

- ✅ **FREE** - 14,400 requests/day
- ✅ **Fast** - Faster than OpenAI
- ✅ **Quality** - Llama 3.3 70B model
- ✅ **No credit card** - Just sign up and go

---

## 🎉 Happy Researching!

You now have an AI investment research agent running for **$0/month**!
