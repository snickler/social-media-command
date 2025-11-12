# AI Model Selection Feature - Implementation Summary

## Overview

Added dynamic AI model selection to the AI Assistant, allowing users to choose which Foundry Local model to use at runtime instead of having a hardcoded default.

## Changes Made

### 1. ViewModel Updates (`AIAssistantViewModel.cs`)
- Added `SelectedModel` property (default: "phi-3.5-mini")
- Added `AvailableModels` ObservableCollection with 6 common models:
  - phi-3.5-mini
  - llama-3.2-1b
  - llama-3.2-3b
  - llama-3.1-8b
  - mistral-7b
  - gemma-2b
- Added model switching logic in `GenerateContentAsync()` via `_aiService.SetModel(SelectedModel)`

### 2. View Updates (`AIAssistantView.axaml`)
- Added "AI Model" ComboBox as first configuration option
- ComboBox is **editable** - users can type custom model names
- Added tooltip: "Select or type a Foundry Local model name (use 'foundry cache list')"
- Updated Grid layout from 3 columns to 4 columns (Model, Tone, Content Type, Options)

### 3. Service Interface (`IAIService.cs`)
- Added `void SetModel(string modelName)` method signature

### 4. Service Implementation (`FoundryLocalAIService.cs`)
- Implemented `SetModel()` to update `_config.ModelName` dynamically
- Added logging when model is changed

### 5. Configuration Model (`AIModels.cs`)
- Changed `ModelName` default from `"phi-3.5-mini"` to `string.Empty`
- Updated documentation comments
- Added system prompt default: "You are a helpful AI assistant that generates engaging social media content."

### 6. Documentation (`FOUNDRY_LOCAL_SETUP.md`)
- Updated "Configuration" section with model selection workflow
- Moved UI-based model selection to "Option A (Recommended)"
- Added model selection tips in testing section
- Added performance characteristics for each model size

## User Experience

### Before
- Model hardcoded to `llama3.2` (which didn't match Foundry Local format)
- Required editing config files or recompiling to change models
- Users got 404 errors with wrong model names

### After
- **Dropdown with 6 common models** for quick selection
- **Editable ComboBox** - type any model name (e.g., from `foundry cache list`)
- **Dynamic switching** - no app restart required
- **Tooltips** guiding users to find available models
- **Logging** shows when model changes

## Technical Details

### Model Validation
- No validation yet - relies on Foundry Local API to return 404 if model doesn't exist
- Future: Could add `foundry cache list` CLI integration to populate available models

### State Management
- Model selection persists during app session
- Resets to "phi-3.5-mini" on app restart
- Future: Could persist selection in user settings

### Performance
- Model switching is instant (no initialization delay)
- Only affects next API call to Foundry Local
- Each model has different inference speeds:
  - phi-3.5-mini: ~1-2 seconds
  - llama-3.2-3b: ~2-4 seconds
  - llama-3.1-8b: ~5-10 seconds

## Testing

### Build Status
✅ Solution builds successfully (11.5s clean build)

### Manual Testing Steps
1. Run app: `dotnet run --project SocialMediaCommander.Desktop`
2. Open AI Assistant (🤖 button)
3. Verify "AI Model" dropdown shows as first option
4. Try selecting different models from dropdown
5. Try typing custom model name
6. Generate content and verify logs show model switch
7. Verify content generates successfully

### Expected Logs
```
info: SocialMediaCommander.Desktop.ViewModels.AIAssistantViewModel[0]
      Model changed to: phi-3.5-mini
info: SocialMediaCommander.Desktop.ViewModels.AIAssistantViewModel[0]
      Using model: phi-3.5-mini
info: SocialMediaCommander.Services.Implementation.FoundryLocalAIService[0]
      AI model updated to: phi-3.5-mini
```

## Future Enhancements

### Short Term
1. Add "Refresh Models" button that runs `foundry cache list` CLI
2. Add model size/speed indicators in dropdown (e.g., "phi-3.5-mini (Fast)")
3. Persist selected model in user preferences

### Medium Term
4. Auto-detect running models via Foundry Local API
5. Show model download status for uncached models
6. Add model performance metrics (tokens/sec)

### Long Term
7. Support model-specific parameters (temperature, top_p per model)
8. Add model comparison mode (generate with multiple models)
9. Integration with model benchmarks for quality estimates

## Migration Notes

### For Existing Users
- First run after update will default to empty model name
- Users must select a model from dropdown
- If using `appsettings.json`, remove or empty `ModelName` field

### For New Users
- No migration needed
- Model selection is part of first-time setup
- Documentation guides users through `foundry cache list` workflow

## Related Files
- `SocialMediaCommander.Desktop/ViewModels/AIAssistantViewModel.cs`
- `SocialMediaCommander.Desktop/Views/AIAssistantView.axaml`
- `SocialMediaCommander.Services/Interfaces/IAIService.cs`
- `SocialMediaCommander.Services/Implementation/FoundryLocalAIService.cs`
- `SocialMediaCommander.Core/Models/AIModels.cs`
- `docs/development/FOUNDRY_LOCAL_SETUP.md`

## References
- [Foundry Local Architecture - Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/concepts/foundry-local-architecture)
- [Foundry Local REST API Reference](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/reference/reference-rest)
