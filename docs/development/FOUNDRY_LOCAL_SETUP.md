# Foundry Local Setup Guide

## Overview

Foundry Local provides local AI model execution with an OpenAI-compatible API. This guide covers setup and configuration for Social Media Commander.

## Prerequisites

- Foundry Local CLI installed
- AI model downloaded (e.g., `phi-3.5-mini`, `llama-3.2-1b`)

## API Endpoint Structure

**Standard format:**
```
http://localhost:PORT/v1/chat/completions
```

Where `PORT` is dynamically assigned by Foundry Local on each service start.

### Available Endpoints

Foundry Local supports OpenAI-compatible endpoints:

| Endpoint | Method | Purpose | Reference |
|----------|--------|---------|-----------|
| `/v1/chat/completions` | POST | Generate chat completions | [Microsoft Docs](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/reference/reference-rest) |
| `/v1/models` | GET | List available models | OpenAI-compatible |
| `/health` | GET | Service health check | Foundry Local specific |

**Model Discovery Example:**
```bash
# Get available models from Foundry Local
curl http://localhost:5273/v1/models

# Response:
{
  "object": "list",
  "data": [
    {
      "id": "phi-3.5-mini",
      "object": "model",
      "created": 1234567890,
      "owned_by": "microsoft"
    }
  ]
}
```

## CLI Commands

### Check Service Status
```bash
foundry service status
```
Shows:
- Whether service is running
- Current port number
- Endpoint URL

### List Available Models
```bash
# List cached models (already downloaded)
foundry cache list

# List all available models for download
foundry model list
```

### Start/Stop Service
```bash
# Start service
foundry service start

# Stop service
foundry service stop
```

## Configuration in Social Media Commander

### 1. Find Your Port
Run `foundry service status` to get the current port:
```
Service running at: http://localhost:5273
```

### 2. Find Your Model Name
Run `foundry cache list` to see downloaded models:
```
phi-3.5-mini
llama-3.2-1b
llama-3.2-3b
```

### 3. Update Configuration

**Option A: Select Model in AI Assistant** (Recommended)
1. Run desktop app
2. Click 🤖 AI Assistant button
3. In the "AI Model" dropdown:
   - **Models are automatically loaded from Foundry Local** via `/v1/models` endpoint
   - Click 🔄 **Refresh** button to update the list
   - Select your preferred model from the dropdown
   - Or type a custom model name (must match exactly from `foundry cache list`)
4. Selected model is used for all AI generation requests

**How Model Discovery Works:**
- On startup, the app queries `GET http://localhost:5273/v1/models`
- Parses the OpenAI-compatible JSON response
- Populates the dropdown with available models
- Falls back to common models if Foundry Local is offline

**Option B: Set Default in `appsettings.json`**
```json
{
  "AIModelConfig": {
    "ModelName": "",
    "BaseUrl": "http://localhost:5273",
    "Temperature": 0.7,
    "MaxTokens": 2048,
    "SystemPrompt": "You are a helpful AI assistant that generates engaging social media content."
  }
}
```
*Note: Leave `ModelName` empty - users select model in UI*

**Option C: Override Base URL Only**
If your Foundry Local runs on a different port:
```json
{
  "AIModelConfig": {
    "BaseUrl": "http://localhost:YOUR_PORT"
  }
}
```

### 4. Verify Connection

The AI Assistant will show:
- ✅ **Success**: Content generated
- ❌ **NotFound (404)**: Wrong endpoint or model name
- ❌ **Connection Refused**: Service not running

## API Request Format

Based on [Microsoft Learn - Foundry Local REST API Reference](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/reference/reference-rest):

**Request:**
```http
POST http://localhost:5273/v1/chat/completions
Content-Type: application/json

{
  "model": "phi-3.5-mini",
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful AI assistant."
    },
    {
      "role": "user",
      "content": "Write a social media post about awesomeness."
    }
  ],
  "temperature": 0.7,
  "max_tokens": 2048,
  "stream": false
}
```

**Response:**
```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "Generated content here..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 20,
    "completion_tokens": 50,
    "total_tokens": 70
  }
}
```

## Troubleshooting

### 404 Not Found
**Causes:**
1. Model name doesn't match exactly (case-sensitive)
2. Model not downloaded/cached
3. Endpoint path incorrect

**Solution:**
```bash
# Verify model exists
foundry cache list

# Ensure exact model name match in config
# ✅ Correct: "phi-3.5-mini"
# ❌ Wrong: "phi-3.5", "Phi-3.5-Mini", "phi3.5-mini"
```

### Connection Refused
**Cause:** Foundry Local service not running

**Solution:**
```bash
# Start service
foundry service start

# Wait 3-5 seconds, then verify
foundry service status
```

### Wrong Port
**Cause:** Port changed after service restart (dynamic assignment)

**Solution:**
1. Check current port: `foundry service status`
2. Update `BaseUrl` in config
3. Restart desktop app

## Implementation Details

### HttpClient Configuration
```csharp
// FoundryLocalAIService.cs sets BaseAddress immediately
_httpClient.BaseAddress = new Uri("http://localhost:5273");

// API calls use relative path
await _httpClient.PostAsync("v1/chat/completions", content);

// Full URL: http://localhost:5273/v1/chat/completions
```

### Authentication
**Default:** No authentication required for local Foundry Local setup.

If authentication is needed (custom setup):
```csharp
_httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer <token>");
```

## Common Model Names

Based on Microsoft documentation and Foundry Local compatibility:

| Model | Size | Best For |
|-------|------|----------|
| `phi-3.5-mini` | Small | Fast inference, general tasks |
| `llama-3.2-1b` | 1B params | Quick responses, mobile-friendly |
| `llama-3.2-3b` | 3B params | Better quality, still fast |
| `llama-3.1-8b` | 8B params | High quality, slower |

**Recommendation for Social Media Commander:** Start with `phi-3.5-mini` for fast, quality content generation.

## References

- [Foundry Local Architecture - Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/concepts/foundry-local-architecture)
- [Foundry Local REST API Reference - Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/reference/reference-rest)
- [OpenAI Chat Completions API](https://platform.openai.com/docs/api-reference/chat/create)

## Testing

Run desktop app and test AI Assistant:
```
1. Click 🤖 AI Assistant button
2. Select AI Model: "phi-3.5-mini" (or your preferred model)
3. Enter prompt: "Write a post about awesomeness"
4. Select tone: Professional
5. Click "Generate Content"
6. Verify generated content appears
```

**Expected behavior:**
- Model dropdown **automatically loads from Foundry Local** (`/v1/models` endpoint)
- Shows only models you have cached/downloaded
- Click 🔄 to refresh model list
- Content generated in 1-5 seconds (depending on model size)
- No HTTP errors
- Clean, engaging social media content

**Model Selection Tips:**
- **phi-3.5-mini**: Fastest, best for quick iterations
- **llama-3.2-1b/3b**: Good balance of speed and quality
- **llama-3.1-8b**: Highest quality, slower generation
- Custom models: Editable dropdown allows typing any model name

**Troubleshooting Model Discovery:**
- **"Loading models..." persists**: Foundry Local service not running (`foundry service start`)
- **Dropdown shows fallback models**: API call failed, check logs
- **Models not appearing**: Run `foundry cache list` to verify models are downloaded
