# LLM Setup Guide - Free Alternatives to OpenAI

Your AIIRA Agent now supports **THREE** LLM providers. This guide will help you choose and configure the best option for your needs.

---

## 🎯 Quick Recommendation

**Use Groq** - It's free, fast, and requires no local installation!

---

## Option 1: Groq (RECOMMENDED) ⭐

### Why Choose Groq?
- ✅ **100% FREE** - No credit card required
- ✅ **Fast inference** - Much faster than OpenAI
- ✅ **Generous limits** - 14,400 requests/day (RPD)
- ✅ **Cloud-based** - No local installation needed
- ✅ **Powerful models** - Llama 3.3 70B and more

### Setup Instructions

1. **Get API Key** (2 minutes)
   - Visit: https://console.groq.com
   - Sign up for free account
   - Navigate to API Keys section
   - Click "Create API Key"
   - Copy your key (starts with `gsk_...`)

2. **Configure Application**
   
   Edit `appsettings.json`:
   ```json
   {
     "LLM": {
       "Provider": "groq"
     },
     "Groq": {
       "ApiKey": "gsk_your_key_here",
       "Model": "llama-3.3-70b-versatile"
     }
   }
   ```

3. **That's it!** Run your application:
   ```bash
   dotnet run --project src/AIRA.Api
   ```

### Available Models
- `llama-3.3-70b-versatile` (Recommended) - Best balance of speed and quality
- `llama-3.1-70b-versatile` - Alternative high-quality model
- `mixtral-8x7b-32768` - Good for longer context
- `gemma2-9b-it` - Faster, lighter model

### Rate Limits (Free Tier)
- 14,400 requests per day
- 30 requests per minute
- More than enough for investment research!

---

## Option 2: Ollama (Local & Free) 🖥️

### Why Choose Ollama?
- ✅ **100% FREE** - No API keys needed
- ✅ **Unlimited usage** - No rate limits
- ✅ **Privacy** - Everything runs locally
- ✅ **Offline capable** - Works without internet
- ❌ **Slower** - Depends on your hardware
- ❌ **Requires setup** - Need to install and download models

### Setup Instructions

1. **Install Ollama**
   - Windows: Download from https://ollama.ai
   - Run the installer
   - Ollama will start automatically

2. **Download Model**
   ```bash
   ollama pull llama3.2
   ```
   
   Or for better quality (larger download):
   ```bash
   ollama pull llama3.1:8b
   ```

3. **Configure Application**
   
   Edit `appsettings.json`:
   ```json
   {
     "LLM": {
       "Provider": "ollama"
     },
     "Ollama": {
       "Model": "llama3.2",
       "BaseUrl": "http://localhost:11434"
     }
   }
   ```

4. **Run your application**:
   ```bash
   dotnet run --project src/AIRA.Api
   ```

### Available Models
- `llama3.2` (Recommended) - Fast and efficient
- `llama3.1:8b` - Better quality, slower
- `mistral` - Alternative option
- `phi3` - Lightweight option

### Hardware Requirements
- **Minimum**: 8GB RAM for llama3.2
- **Recommended**: 16GB RAM for llama3.1:8b
- GPU is optional but speeds up inference

---

## Option 3: OpenAI (Paid) 💳

### Why Choose OpenAI?
- ✅ **Highest quality** - Best reasoning capabilities
- ✅ **Most reliable** - Proven at scale
- ❌ **COSTS MONEY** - Requires credits
- ❌ **Rate limits** - Based on your plan

### Setup Instructions

1. **Get API Key**
   - Visit: https://platform.openai.com/api-keys
   - Add credits to your account (REQUIRED)
   - Create API key

2. **Configure Application**
   
   Edit `appsettings.json`:
   ```json
   {
     "LLM": {
       "Provider": "openai"
     },
     "OpenAI": {
       "ApiKey": "sk-your-key-here",
       "Model": "gpt-4o-mini"
     }
   }
   ```

### Recommended Models
- `gpt-4o-mini` - Most cost-effective ($0.15/1M tokens)
- `gpt-4o` - Better quality, more expensive
- `gpt-3.5-turbo` - Cheapest option

---

## 🔄 Switching Between Providers

You can switch providers anytime by changing the `LLM:Provider` setting:

```json
{
  "LLM": {
    "Provider": "groq"  // or "ollama" or "openai"
  }
}
```

---

## 🧪 Testing Your Setup

After configuration, test your setup:

```bash
# Start the API
dotnet run --project src/AIRA.Api

# In another terminal, submit a test job
curl -X POST http://localhost:5000/api/analysis \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Apple Inc",
    "symbol": "AAPL",
    "analysisDepth": "quick"
  }'
```

Check the logs for:
```
[INFO] LLM Provider: groq
[INFO] Groq LLM client configured (FREE) with model: llama-3.3-70b-versatile
```

---

## 🆘 Troubleshooting

### Groq Issues

**Problem**: "API key invalid"
- **Solution**: Check your API key starts with `gsk_`
- Regenerate key at https://console.groq.com

**Problem**: "Rate limit exceeded"
- **Solution**: Wait a minute or switch to Ollama for unlimited usage

### Ollama Issues

**Problem**: "Cannot connect to Ollama"
- **Solution**: Check Ollama is running: `ollama list`
- Restart Ollama app from system tray

**Problem**: "Model not found"
- **Solution**: Download model: `ollama pull llama3.2`

**Problem**: "Out of memory"
- **Solution**: Use smaller model: `ollama pull phi3`

### OpenAI Issues

**Problem**: "Insufficient quota"
- **Solution**: Add credits or switch to Groq (free!)

---

## 📊 Comparison Table

| Feature | Groq | Ollama | OpenAI |
|---------|------|--------|--------|
| **Cost** | Free | Free | Paid |
| **Speed** | Fast | Medium | Fast |
| **Quality** | High | High | Highest |
| **Setup** | Easy | Medium | Easy |
| **Privacy** | Cloud | Local | Cloud |
| **Rate Limits** | 14k/day | Unlimited | Varies |
| **Best For** | Most users | Privacy-focused | Enterprise |

---

## 🎉 Recommendation Summary

1. **Start with Groq** - Free, fast, no installation
2. **Use Ollama if** - You need unlimited usage or privacy
3. **Use OpenAI if** - You need the absolute best quality and don't mind paying

---

## 💡 Tips

- **For development**: Use Groq (fast iteration, free)
- **For production**: Consider Groq or OpenAI based on your needs
- **For sensitive data**: Use Ollama (everything stays local)
- **Save money**: Use Groq instead of OpenAI (it's really good!)

---

## 📝 Next Steps

1. Choose your provider (we recommend Groq!)
2. Follow the setup instructions above
3. Update your `appsettings.json`
4. Run the application
5. Enjoy free AI-powered investment research! 🚀

---

## 🔗 Useful Links

- Groq Console: https://console.groq.com
- Ollama Website: https://ollama.ai
- OpenAI Platform: https://platform.openai.com
- AIIRA Documentation: See README.md

---

**Questions?** Check the troubleshooting section or open an issue on GitHub.
