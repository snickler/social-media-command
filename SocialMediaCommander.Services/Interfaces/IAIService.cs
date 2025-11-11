using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// AI service interface for content generation and optimization
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Generate content using AI based on prompt and parameters
    /// </summary>
    Task<AIContentResponse> GenerateContentAsync(AIContentRequest request);

    /// <summary>
    /// Optimize existing content for better engagement
    /// </summary>
    Task<AIOptimizationResponse> OptimizeContentAsync(AIOptimizationRequest request);

    /// <summary>
    /// Analyze content and provide insights
    /// </summary>
    Task<AIAnalysis> AnalyzeContentAsync(string content, SocialPlatform platform);

    /// <summary>
    /// Generate hashtags for content
    /// </summary>
    Task<List<string>> GenerateHashtagsAsync(string content, int maxCount = 10);

    /// <summary>
    /// Get AI-powered insights and recommendations
    /// </summary>
    Task<AIInsights> GetInsightsAsync(string userId);

    /// <summary>
    /// Predict content performance
    /// </summary>
    Task<AIPerformancePrediction> PredictPerformanceAsync(Post post);

    /// <summary>
    /// Generate content variations for A/B testing
    /// </summary>
    Task<List<AIGeneratedContent>> GenerateVariationsAsync(string content, int count = 3);

    /// <summary>
    /// Suggest optimal posting times
    /// </summary>
    Task<Dictionary<SocialPlatform, DateTime>> SuggestOptimalPostingTimesAsync(string userId);

    /// <summary>
    /// Generate thread content from a single post
    /// </summary>
    Task<List<string>> GenerateThreadAsync(string content, int maxPosts = 5);

    /// <summary>
    /// Translate content to different languages
    /// </summary>
    Task<Dictionary<string, string>> TranslateContentAsync(string content, List<string> targetLanguages);

    /// <summary>
    /// Check if AI service is available
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get AI model information
    /// </summary>
    Task<AIModelConfig> GetModelInfoAsync();

    /// <summary>
    /// Update the AI model to use for generation
    /// </summary>
    void SetModel(string modelName);

    /// <summary>
    /// Get the list of available models from the AI service.
    /// For Foundry Local, this queries the /v1/models endpoint.
    /// </summary>
    /// <returns>A list of available model names.</returns>
    Task<List<string>> GetAvailableModelsAsync();

    /// <summary>
    /// Generate content using AI based on simple prompt
    /// </summary>
    Task<string> GenerateContentAsync(string prompt);
}