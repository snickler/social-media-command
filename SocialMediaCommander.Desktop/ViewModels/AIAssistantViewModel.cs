using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for AI Assistant functionality
/// </summary>
public class AIAssistantViewModel : INotifyPropertyChanged
{
    private readonly IAIService _aiService;
    private readonly ILogger<AIAssistantViewModel> _logger;

    private string _prompt = string.Empty;
    private string _generatedContent = string.Empty;
    private bool _isGenerating = false;
    private AITone _selectedTone = AITone.Professional;
    private AIContentType _selectedContentType = AIContentType.Post;
    private bool _includeHashtags = true;
    private bool _includeEmojis = false;
    private string _brandVoice = string.Empty;
    private string _keywords = string.Empty;
    private string _selectedModel = "phi-3.5-mini";
    private AIContentResponse? _lastResponse;

    public AIAssistantViewModel(IAIService aiService, ILogger<AIAssistantViewModel> logger)
    {
        _aiService = aiService;
        _logger = logger;

        GenerateContentCommand = new AsyncRelayCommand(GenerateContentAsync, () => !IsGenerating && !string.IsNullOrWhiteSpace(Prompt));
        OptimizeContentCommand = new AsyncRelayCommand(OptimizeContentAsync, () => !IsGenerating && !string.IsNullOrWhiteSpace(GeneratedContent));
        GenerateVariationsCommand = new AsyncRelayCommand(GenerateVariationsAsync, () => !IsGenerating && !string.IsNullOrWhiteSpace(GeneratedContent));
        GenerateHashtagsCommand = new AsyncRelayCommand(GenerateHashtagsAsync, () => !IsGenerating && !string.IsNullOrWhiteSpace(GeneratedContent));
        GenerateThreadCommand = new AsyncRelayCommand(GenerateThreadAsync, () => !IsGenerating && !string.IsNullOrWhiteSpace(GeneratedContent));
        RefreshAvailableModelsCommand = new AsyncRelayCommand(LoadAvailableModelsAsync);

        TargetPlatforms = new ObservableCollection<PlatformSelectionItem>
        {
            // TODO: Add Twitter, LinkedIn, Facebook, Threads when implementations are ready
            new() { Platform = SocialPlatform.BlueSky, IsSelected = true }
        };

        // Available Foundry Local models - loaded from service
        AvailableModels = new ObservableCollection<string>
        {
            "Loading models..."
        };

        // Load available models from Foundry Local asynchronously
        _ = LoadAvailableModelsAsync();

        ContentVariations = new ObservableCollection<AIGeneratedContent>();
        GeneratedHashtags = new ObservableCollection<string>();
        ThreadPosts = new ObservableCollection<string>();
        OptimizationSuggestions = new ObservableCollection<AIOptimizationSuggestion>();
    }

    #region Properties

    public string Prompt
    {
        get => _prompt;
        set
        {
            _prompt = value;
            OnPropertyChanged();
            GenerateContentCommand.NotifyCanExecuteChanged();
        }
    }

    public string GeneratedContent
    {
        get => _generatedContent;
        set
        {
            _generatedContent = value;
            OnPropertyChanged();
            OptimizeContentCommand.NotifyCanExecuteChanged();
            GenerateVariationsCommand.NotifyCanExecuteChanged();
            GenerateHashtagsCommand.NotifyCanExecuteChanged();
            GenerateThreadCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        set
        {
            _isGenerating = value;
            OnPropertyChanged();
            GenerateContentCommand.NotifyCanExecuteChanged();
            OptimizeContentCommand.NotifyCanExecuteChanged();
            GenerateVariationsCommand.NotifyCanExecuteChanged();
            GenerateHashtagsCommand.NotifyCanExecuteChanged();
            GenerateThreadCommand.NotifyCanExecuteChanged();
        }
    }

    public AITone SelectedTone
    {
        get => _selectedTone;
        set
        {
            _selectedTone = value;
            OnPropertyChanged();
        }
    }

    public AIContentType SelectedContentType
    {
        get => _selectedContentType;
        set
        {
            _selectedContentType = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeHashtags
    {
        get => _includeHashtags;
        set
        {
            _includeHashtags = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeEmojis
    {
        get => _includeEmojis;
        set
        {
            _includeEmojis = value;
            OnPropertyChanged();
        }
    }

    public string BrandVoice
    {
        get => _brandVoice;
        set
        {
            _brandVoice = value;
            OnPropertyChanged();
        }
    }

    public string Keywords
    {
        get => _keywords;
        set
        {
            _keywords = value;
            OnPropertyChanged();
        }
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set
        {
            _selectedModel = value;
            OnPropertyChanged();
            _logger.LogInformation("Model changed to: {Model}", value);
        }
    }

    public ObservableCollection<string> AvailableModels { get; }
    public ObservableCollection<PlatformSelectionItem> TargetPlatforms { get; }
    public ObservableCollection<AIGeneratedContent> ContentVariations { get; }
    public ObservableCollection<string> GeneratedHashtags { get; }
    public ObservableCollection<string> ThreadPosts { get; }
    public ObservableCollection<AIOptimizationSuggestion> OptimizationSuggestions { get; }

    public Array AITones => Enum.GetValues<AITone>();
    public Array AIContentTypes => Enum.GetValues<AIContentType>();

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateContentCommand { get; }
    public AsyncRelayCommand OptimizeContentCommand { get; }
    public AsyncRelayCommand GenerateVariationsCommand { get; }
    public AsyncRelayCommand GenerateHashtagsCommand { get; }
    public AsyncRelayCommand GenerateThreadCommand { get; }
    public AsyncRelayCommand RefreshAvailableModelsCommand { get; }

    #endregion

    #region Methods

    private async Task GenerateContentAsync()
    {
        try
        {
            IsGenerating = true;

            // Update AI service model configuration before generating
            _aiService.SetModel(SelectedModel);
            _logger.LogInformation("Using model: {Model}", SelectedModel);

            var request = new AIContentRequest
            {
                Prompt = Prompt,
                ContentType = SelectedContentType,
                Tone = SelectedTone,
                IncludeHashtags = IncludeHashtags,
                IncludeEmojis = IncludeEmojis,
                BrandVoice = string.IsNullOrWhiteSpace(BrandVoice) ? null : BrandVoice,
                Keywords = Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(k => k.Trim()).ToList(),
                TargetPlatforms = TargetPlatforms.Where(p => p.IsSelected).Select(p => p.Platform).ToList()
            };

            _lastResponse = await _aiService.GenerateContentAsync(request);

            if (_lastResponse.Success && _lastResponse.GeneratedContent.Any())
            {
                GeneratedContent = _lastResponse.GeneratedContent.First().Content;

                // Update collections
                ContentVariations.Clear();
                foreach (var content in _lastResponse.GeneratedContent)
                {
                    ContentVariations.Add(content);
                }
            }
            else
            {
                GeneratedContent = $"Error: {_lastResponse.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI content");
            GeneratedContent = $"Error: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task OptimizeContentAsync()
    {
        try
        {
            IsGenerating = true;

            var selectedPlatform = TargetPlatforms.FirstOrDefault(p => p.IsSelected)?.Platform ?? SocialPlatform.BlueSky;

            var request = new AIOptimizationRequest
            {
                Content = GeneratedContent,
                Platform = selectedPlatform,
                OptimizationType = AIOptimizationType.Engagement
            };

            var response = await _aiService.OptimizeContentAsync(request);
            GeneratedContent = response.OptimizedContent;

            OptimizationSuggestions.Clear();
            foreach (var suggestion in response.Suggestions)
            {
                OptimizationSuggestions.Add(suggestion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing content");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task GenerateVariationsAsync()
    {
        try
        {
            IsGenerating = true;

            var variations = await _aiService.GenerateVariationsAsync(GeneratedContent, 3);

            ContentVariations.Clear();
            foreach (var variation in variations)
            {
                ContentVariations.Add(variation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content variations");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task GenerateHashtagsAsync()
    {
        try
        {
            IsGenerating = true;

            var hashtags = await _aiService.GenerateHashtagsAsync(GeneratedContent, 10);

            GeneratedHashtags.Clear();
            foreach (var hashtag in hashtags)
            {
                GeneratedHashtags.Add($"#{hashtag}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtags");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task GenerateThreadAsync()
    {
        try
        {
            IsGenerating = true;

            var threadPosts = await _aiService.GenerateThreadAsync(GeneratedContent, 5);

            ThreadPosts.Clear();
            foreach (var post in threadPosts)
            {
                ThreadPosts.Add(post);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thread");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task LoadAvailableModelsAsync()
    {
        try
        {
            _logger.LogInformation("Loading available models from Foundry Local");

            // Get available models from AI service
            var models = await _aiService.GetAvailableModelsAsync();

            AvailableModels.Clear();

            if (models?.Any() == true)
            {
                foreach (var model in models.OrderBy(m => m))
                {
                    AvailableModels.Add(model);
                    _logger.LogDebug("Found model: {Model}", model);
                }

                // Select first model if none selected
                if (string.IsNullOrEmpty(SelectedModel) || SelectedModel == "Loading models...")
                {
                    SelectedModel = AvailableModels.First();
                }

                _logger.LogInformation("Loaded {Count} models from Foundry Local", models.Count);
            }
            else
            {
                // Fallback to common models if service doesn't return any
                _logger.LogWarning("No models returned from service, using fallback list");
                AvailableModels.Add("phi-3.5-mini");
                AvailableModels.Add("llama-3.2-1b");
                AvailableModels.Add("llama-3.2-3b");
                AvailableModels.Add("llama-3.1-8b");
                AvailableModels.Add("mistral-7b");
                AvailableModels.Add("gemma-2b");

                if (string.IsNullOrEmpty(SelectedModel) || SelectedModel == "Loading models...")
                {
                    SelectedModel = "phi-3.5-mini";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available models");

            // Fallback to common models on error
            AvailableModels.Clear();
            AvailableModels.Add("phi-3.5-mini");
            AvailableModels.Add("llama-3.2-1b");
            AvailableModels.Add("llama-3.2-3b");
            AvailableModels.Add("llama-3.1-8b");
            AvailableModels.Add("mistral-7b");
            AvailableModels.Add("gemma-2b");

            if (string.IsNullOrEmpty(SelectedModel) || SelectedModel == "Loading models...")
            {
                SelectedModel = "phi-3.5-mini";
            }
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

/// <summary>
/// Helper class for platform selection
/// </summary>
public class PlatformSelectionItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public SocialPlatform Platform { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string DisplayName => Platform.ToString();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}