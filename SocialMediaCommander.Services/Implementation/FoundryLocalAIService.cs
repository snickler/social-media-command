using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using System.Diagnostics;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// AI service implementation using Foundry Local with OpenAI-compatible API
/// Based on Microsoft Foundry Local documentation
/// </summary>
public class FoundryLocalAIService : IAIService
{
    private readonly ILogger<FoundryLocalAIService> _logger;
    private readonly AIModelConfig _config;
    private readonly HttpClient _httpClient;
    private string? _serviceEndpoint;
    private bool _serviceInitialized = false;
    
    public FoundryLocalAIService(
        ILogger<FoundryLocalAIService> logger,
        IOptions<AIModelConfig> config,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config.Value;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // AI generation can take time
    }

    /// <summary>
    /// Initializes the Foundry Local service and discovers the endpoint
    /// </summary>
    private async Task InitializeServiceAsync()
    {
        if (_serviceInitialized && !string.IsNullOrEmpty(_serviceEndpoint))
            return;

        try
        {
            // Check if Foundry Local service is running and get endpoint
            var endpoint = await DiscoverServiceEndpointAsync();
            if (!string.IsNullOrEmpty(endpoint))
            {
                _serviceEndpoint = endpoint;
                _httpClient.BaseAddress = new Uri(endpoint);
                _serviceInitialized = true;
                _logger.LogInformation("Foundry Local service initialized at: {Endpoint}", endpoint);
            }
            else
            {
                // Try to start the service if not running
                await StartFoundryServiceAsync();
                endpoint = await DiscoverServiceEndpointAsync();
                if (!string.IsNullOrEmpty(endpoint))
                {
                    _serviceEndpoint = endpoint;
                    _httpClient.BaseAddress = new Uri(endpoint);
                    _serviceInitialized = true;
                    _logger.LogInformation("Foundry Local service started and initialized at: {Endpoint}", endpoint);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Foundry Local service");
            // Fallback to configured base URL if available
            if (!string.IsNullOrEmpty(_config.BaseUrl))
            {
                _serviceEndpoint = _config.BaseUrl;
                _httpClient.BaseAddress = new Uri(_config.BaseUrl);
                _serviceInitialized = true;
                _logger.LogWarning("Using fallback endpoint: {Endpoint}", _config.BaseUrl);
            }
        }
    }

    /// <summary>
    /// Discovers the Foundry Local service endpoint using CLI
    /// </summary>
    private async Task<string?> DiscoverServiceEndpointAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "foundry",
                Arguments = "service status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                
                // Parse the output to extract the endpoint
                // Example output: "Service running at: http://localhost:8080"
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("running at:", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("endpoint:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            var endpoint = string.Join(":", parts.Skip(1)).Trim();
                            if (Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                            {
                                return endpoint.EndsWith("/v1") ? endpoint : $"{endpoint}/v1";
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not discover Foundry Local endpoint via CLI");
        }

        return null;
    }

    /// <summary>
    /// Attempts to start the Foundry Local service
    /// </summary>
    private async Task StartFoundryServiceAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "foundry",
                Arguments = "service start",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                // Give the service time to start
                await Task.Delay(3000);
                _logger.LogInformation("Attempted to start Foundry Local service");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not start Foundry Local service via CLI");
        }
    }

    public async Task<AIContentResponse> GenerateContentAsync(AIContentRequest request)
    {
        _logger.LogInformation("Generating AI content for request: {RequestId}", request.Id);
        
        try
        {
            await InitializeServiceAsync();
            
            var startTime = DateTime.UtcNow;
            
            // Build platform-specific prompt
            var platformInfo = GetPlatformInfo(request.TargetPlatforms);
            var prompt = BuildContentGenerationPrompt(request, platformInfo);
            
            var response = await CallFoundryLocalAsync(prompt);
            
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (string.IsNullOrWhiteSpace(response))
            {
                return new AIContentResponse
                {
                    Id = request.Id,
                    Success = false,
                    ErrorMessage = "Failed to generate content from AI model",
                    GeneratedAt = DateTime.UtcNow
                };
            }

            // Parse and structure the response
            var generatedContent = ParseGeneratedContent(response, request);
            
            return new AIContentResponse
            {
                Id = request.Id,
                Success = true,
                GeneratedContent = generatedContent,
                Metrics = new AIMetrics
                {
                    ProcessingTimeMs = processingTime,
                    ModelUsed = _config.ModelName,
                    TokensUsed = EstimateTokens(prompt + response),
                    SentimentScore = AnalyzeSentiment(response),
                    DetectedTopics = ExtractTopics(response),
                    EngagementPrediction = PredictEngagement(response, request.TargetPlatforms)
                },
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI content");
            return new AIContentResponse
            {
                Id = request.Id,
                Success = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<AIOptimizationResponse> OptimizeContentAsync(AIOptimizationRequest request)
    {
        _logger.LogInformation("Optimizing content for platform: {Platform}", request.Platform);
        
        try
        {
            await InitializeServiceAsync();
            
            var platformLimits = GetPlatformLimits(request.Platform);
            var prompt = BuildOptimizationPrompt(request, platformLimits);
            
            var response = await CallFoundryLocalAsync(prompt);
            
            if (string.IsNullOrWhiteSpace(response))
            {
                return new AIOptimizationResponse
                {
                    OptimizedContent = request.Content,
                    ImprovementScore = 0.0,
                    Suggestions = new List<AIOptimizationSuggestion>
                    {
                        new AIOptimizationSuggestion
                        {
                            Type = "Error",
                            Suggestion = "Failed to optimize content",
                            Reason = "AI service unavailable",
                            Impact = 0.0
                        }
                    }
                };
            }

            return ParseOptimizationResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing content");
            return new AIOptimizationResponse
            {
                OptimizedContent = request.Content,
                ImprovementScore = 0.0,
                Suggestions = new List<AIOptimizationSuggestion>
                {
                    new AIOptimizationSuggestion
                    {
                        Type = "Error",
                        Suggestion = ex.Message,
                        Reason = "System error",
                        Impact = 0.0
                    }
                }
            };
        }
    }

    public async Task<AIAnalysis> AnalyzeContentAsync(string content, SocialPlatform platform)
    {
        _logger.LogInformation("Analyzing content for platform: {Platform}", platform);
        
        try
        {
            await InitializeServiceAsync();
            
            var prompt = BuildAnalysisPrompt(content, platform);
            var response = await CallFoundryLocalAsync(prompt);
            
            return ParseAnalysisResponse(response, content, platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content");
            return new AIAnalysis
            {
                ReadabilityScore = 50.0,
                SentimentScore = 0.5,
                EngagementPotential = 0.5,
                RecommendedHashtags = new List<string>(),
                OptimalPostTime = "12:00 PM"
            };
        }
    }

    public async Task<List<string>> GenerateHashtagsAsync(string content, int maxCount = 10)
    {
        _logger.LogInformation("Generating hashtags for content");
        
        try
        {
            await InitializeServiceAsync();
            
            var prompt = BuildHashtagPrompt(content, maxCount);
            var response = await CallFoundryLocalAsync(prompt);
            
            return ParseHashtags(response, maxCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtags");
            return new List<string> { "content", "social", "media" };
        }
    }

    public async Task<AIInsights> GetInsightsAsync(string userId)
    {
        _logger.LogInformation("Getting AI insights for user: {UserId}", userId);
        
        try
        {
            await InitializeServiceAsync();
            
            var prompt = BuildInsightsPrompt(userId);
            var response = await CallFoundryLocalAsync(prompt);
            
            return ParseInsightsResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights");
            return new AIInsights
            {
                TrendAnalysis = new List<AITrendAnalysis>
                {
                    new AITrendAnalysis { Topic = "Technology", TrendScore = 0.8 },
                    new AITrendAnalysis { Topic = "Social Media", TrendScore = 0.7 },
                    new AITrendAnalysis { Topic = "AI", TrendScore = 0.9 }
                },
                AudienceInsights = new List<AIAudienceInsight>
                {
                    new AIAudienceInsight { Demographic = "Tech Professionals", EngagementRate = 0.75 }
                },
                ContentRecommendations = new List<AIContentRecommendation>
                {
                    new AIContentRecommendation { ContentType = "Educational", PotentialReach = 1000 }
                }
            };
        }
    }

    public async Task<AIPerformancePrediction> PredictPerformanceAsync(Post post)
    {
        _logger.LogInformation("Predicting performance for post: {PostId}", post.Id);
        
        try
        {
            await InitializeServiceAsync();
            
            var prompt = BuildPerformancePredictionPrompt(post);
            var response = await CallFoundryLocalAsync(prompt);
            
            return ParsePerformancePrediction(response, post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting performance");
            return new AIPerformancePrediction
            {
                PredictedEngagement = 0.5,
                PredictedReach = 1000,
                ConfidenceLevel = 0.3,
                Factors = new List<string> { "Unable to analyze" },
                PlatformPerformance = new Dictionary<SocialPlatform, double>
                {
                    { SocialPlatform.X, 0.6 },
                    { SocialPlatform.LinkedIn, 0.4 }
                }
            };
        }
    }

    public async Task<List<AIGeneratedContent>> GenerateVariationsAsync(string content, int count = 3)
    {
        _logger.LogInformation("Generating {Count} variations of content", count);
        
        try
        {
            await InitializeServiceAsync();
            
            var prompt = BuildVariationsPrompt(content, count);
            var response = await CallFoundryLocalAsync(prompt);
            
            return ParseVariations(response, content, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating variations");
            return new List<AIGeneratedContent>
            {
                new AIGeneratedContent
                {
                    Content = content,
                    ConfidenceScore = 0.5,
                    Hashtags = new List<string> { "content" },
                    Platform = SocialPlatform.X
                }
            };
        }
    }

    public async Task<Dictionary<SocialPlatform, DateTime>> SuggestOptimalPostingTimesAsync(string userId)
    {
        _logger.LogInformation("Suggesting optimal posting times for user: {UserId}", userId);
        
        try
        {
            await InitializeServiceAsync();
            
            var prompt = BuildOptimalTimesPrompt(userId);
            var response = await CallFoundryLocalAsync(prompt);
            
            return ParseOptimalTimes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting optimal times");
            return new Dictionary<SocialPlatform, DateTime>
            {
                { SocialPlatform.X, DateTime.Now.AddHours(2) },
                { SocialPlatform.LinkedIn, DateTime.Now.AddHours(1) },
                { SocialPlatform.Facebook, DateTime.Now.AddHours(3) }
            };
        }
    }

    public async Task<List<string>> GenerateThreadAsync(string content, int maxPosts = 5)
    {
        _logger.LogInformation("Generating thread with max {MaxPosts} posts", maxPosts);
        
        try
        {
            await InitializeServiceAsync();
            
            var prompt = BuildThreadPrompt(content, maxPosts);
            var response = await CallFoundryLocalAsync(prompt);
            
            return ParseThread(response, maxPosts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thread");
            return new List<string> { content };
        }
    }

    public async Task<Dictionary<string, string>> TranslateContentAsync(string content, List<string> targetLanguages)
    {
        _logger.LogInformation("Translating content to {Count} languages", targetLanguages.Count);
        
        var translations = new Dictionary<string, string>();
        
        try
        {
            await InitializeServiceAsync();
            
            foreach (var language in targetLanguages)
            {
                var prompt = BuildTranslationPrompt(content, language);
                var response = await CallFoundryLocalAsync(prompt);
                translations[language] = string.IsNullOrWhiteSpace(response) ? content : response.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating content");
            // Return original content for all languages as fallback
            foreach (var language in targetLanguages)
            {
                translations[language] = content;
            }
        }
        
        return translations;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await InitializeServiceAsync();
            
            if (string.IsNullOrEmpty(_serviceEndpoint))
                return false;

            // Test the service with a simple request
            var testRequest = new
            {
                model = _config.ModelName ?? "phi-3-mini",
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                },
                max_tokens = 10
            };

            var json = JsonSerializer.Serialize(testRequest);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/chat/completions", httpContent);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Foundry Local service availability check failed");
            return false;
        }
    }

    public async Task<AIModelConfig> GetModelInfoAsync()
    {
        try
        {
            await InitializeServiceAsync();
            return _config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model info");
            return _config;
        }
    }

    /// <summary>
    /// Calls the Foundry Local OpenAI-compatible API
    /// </summary>
    private async Task<string> CallFoundryLocalAsync(string prompt)
    {
        try
        {
            if (string.IsNullOrEmpty(_serviceEndpoint))
            {
                throw new InvalidOperationException("Foundry Local service not initialized");
            }

            var requestBody = new
            {
                model = _config.ModelName ?? "phi-3-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful AI assistant specialized in social media content creation and optimization." },
                    new { role = "user", content = prompt }
                },
                max_tokens = _config.MaxTokens,
                temperature = _config.Temperature,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Calling Foundry Local API at: {Endpoint}/chat/completions", _serviceEndpoint);
            
            var response = await _httpClient.PostAsync("/chat/completions", httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Foundry Local API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Foundry Local API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseContent);
            
            return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Foundry Local API");
            throw;
        }
    }

    #region Private Helper Methods

    private string BuildContentGenerationPrompt(AIContentRequest request, string platformInfo)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"{_config.SystemPrompt}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Create {request.ContentType.ToString().ToLower()} content with a {request.Tone.ToString().ToLower()} tone.");
        promptBuilder.AppendLine($"Topic: {request.Prompt}");
        promptBuilder.AppendLine($"Platform constraints: {platformInfo}");
        
        if (request.IncludeHashtags)
            promptBuilder.AppendLine("Include relevant hashtags.");
        
        if (request.IncludeEmojis)
            promptBuilder.AppendLine("Include appropriate emojis.");
            
        if (!string.IsNullOrWhiteSpace(request.BrandVoice))
            promptBuilder.AppendLine($"Brand voice: {request.BrandVoice}");
            
        if (request.Keywords?.Any() == true)
            promptBuilder.AppendLine($"Keywords to include: {string.Join(", ", request.Keywords)}");
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate engaging, platform-appropriate content:");

        return promptBuilder.ToString();
    }

    private string GetPlatformInfo(List<SocialPlatform> platforms)
    {
        var info = new List<string>();
        
        foreach (var platform in platforms)
        {
            info.Add(platform switch
            {
                SocialPlatform.X => "X/Twitter: 280 characters, casual tone",
                SocialPlatform.LinkedIn => "LinkedIn: 3000 characters, professional tone",
                SocialPlatform.Facebook => "Facebook: No strict limit, conversational tone",
                SocialPlatform.BlueSky => "BlueSky: 300 characters, community-focused",
                SocialPlatform.Threads => "Threads: 500 characters, visual-friendly",
                _ => $"{platform}: Standard social media format"
            });
        }
        
        return string.Join("; ", info);
    }

    private PlatformLimits GetPlatformLimits(SocialPlatform platform)
    {
        return platform switch
        {
            SocialPlatform.X => new PlatformLimits { CharacterLimit = 280, MaxMediaCount = 4 },
            SocialPlatform.LinkedIn => new PlatformLimits { CharacterLimit = 3000, MaxMediaCount = 9 },
            SocialPlatform.Facebook => new PlatformLimits { CharacterLimit = 63206, MaxMediaCount = 10 },
            SocialPlatform.BlueSky => new PlatformLimits { CharacterLimit = 300, MaxMediaCount = 4 },
            SocialPlatform.Threads => new PlatformLimits { CharacterLimit = 500, MaxMediaCount = 10 },
            _ => new PlatformLimits { CharacterLimit = 280, MaxMediaCount = 4 }
        };
    }

    private List<AIGeneratedContent> ParseGeneratedContent(string response, AIContentRequest request)
    {
        var content = new AIGeneratedContent
        {
            Content = response.Trim(),
            CharacterCount = response.Length,
            ConfidenceScore = 0.8,
            Platform = request.TargetPlatforms.FirstOrDefault(),
            Hashtags = ExtractHashtags(response),
            Suggestions = new List<string> { "Content generated successfully" }
        };
        
        return new List<AIGeneratedContent> { content };
    }

    private List<string> ExtractHashtags(string content)
    {
        var hashtags = new List<string>();
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words)
        {
            if (word.StartsWith("#") && word.Length > 1)
            {
                hashtags.Add(word.Substring(1));
            }
        }
        
        return hashtags;
    }

    private int EstimateTokens(string text)
    {
        // Rough estimation: ~4 characters per token
        return text.Length / 4;
    }

    private double AnalyzeSentiment(string content)
    {
        // Simple sentiment analysis based on keywords
        var positiveWords = new[] { "great", "excellent", "amazing", "wonderful", "fantastic", "good", "best", "love", "awesome" };
        var negativeWords = new[] { "bad", "terrible", "awful", "hate", "worst", "horrible", "disappointing", "poor" };
        
        var words = content.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var positiveCount = words.Count(w => positiveWords.Contains(w));
        var negativeCount = words.Count(w => negativeWords.Contains(w));
        
        if (positiveCount == 0 && negativeCount == 0) return 0.5;
        
        return (double)positiveCount / (positiveCount + negativeCount);
    }

    private List<string> ExtractTopics(string content)
    {
        // Simple topic extraction based on common keywords
        var topics = new List<string>();
        var contentLower = content.ToLower();
        
        var topicKeywords = new Dictionary<string, string[]>
        {
            ["Technology"] = new[] { "tech", "ai", "software", "digital", "computer", "internet" },
            ["Business"] = new[] { "business", "marketing", "sales", "entrepreneur", "startup" },
            ["Social Media"] = new[] { "social", "media", "content", "engagement", "followers" },
            ["Education"] = new[] { "learn", "education", "training", "course", "knowledge" },
            ["Health"] = new[] { "health", "fitness", "wellness", "medical", "care" }
        };
        
        foreach (var topic in topicKeywords)
        {
            if (topic.Value.Any(keyword => contentLower.Contains(keyword)))
            {
                topics.Add(topic.Key);
            }
        }
        
        return topics.Any() ? topics : new List<string> { "General" };
    }

    private double PredictEngagement(string content, List<SocialPlatform> platforms)
    {
        // Simple engagement prediction based on content characteristics
        var score = 0.5; // Base score
        
        // Length factor
        if (content.Length > 50 && content.Length < 200) score += 0.1;
        
        // Hashtag factor
        var hashtagCount = ExtractHashtags(content).Count;
        if (hashtagCount > 0 && hashtagCount <= 5) score += 0.1;
        
        // Question factor
        if (content.Contains("?")) score += 0.1;
        
        // Call to action
        var ctaWords = new[] { "click", "visit", "check", "read", "watch", "follow", "share" };
        if (ctaWords.Any(word => content.ToLower().Contains(word))) score += 0.1;
        
        return Math.Min(1.0, score);
    }

    // Additional parsing methods would be implemented here
    private string BuildOptimizationPrompt(AIOptimizationRequest request, PlatformLimits limits) => 
        $"Optimize this content for {request.Platform}: {request.Content}\nCharacter limit: {limits.CharacterLimit}\nFocus on: {request.OptimizationType}";

    private AIOptimizationResponse ParseOptimizationResponse(string response, AIOptimizationRequest request) =>
        new AIOptimizationResponse
        {
            OptimizedContent = response.Trim(),
            ImprovementScore = 0.8,
            Suggestions = new List<AIOptimizationSuggestion>()
        };

    private string BuildAnalysisPrompt(string content, SocialPlatform platform) =>
        $"Analyze this {platform} content for readability, sentiment, and engagement potential: {content}";

    private AIAnalysis ParseAnalysisResponse(string response, string content, SocialPlatform platform) =>
        new AIAnalysis
        {
            ReadabilityScore = 75.0,
            SentimentScore = AnalyzeSentiment(content),
            EngagementPotential = PredictEngagement(content, new List<SocialPlatform> { platform }),
            RecommendedHashtags = ExtractHashtags(response),
            OptimalPostTime = "2:00 PM"
        };

    private string BuildHashtagPrompt(string content, int maxCount) =>
        $"Generate {maxCount} relevant hashtags for this content: {content}";

    private List<string> ParseHashtags(string response, int maxCount)
    {
        var hashtags = ExtractHashtags(response);
        if (hashtags.Count == 0)
        {
            // Fallback: extract from response text
            hashtags = response.Split(new[] { '\n', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                              .Where(w => w.Length > 2 && !w.StartsWith("#"))
                              .Take(maxCount)
                              .ToList();
        }
        return hashtags.Take(maxCount).ToList();
    }

    private string BuildInsightsPrompt(string userId) =>
        "Provide social media insights including trending topics, audience preferences, and content recommendations.";

    private AIInsights ParseInsightsResponse(string response) =>
        new AIInsights
        {
            TrendAnalysis = new List<AITrendAnalysis>
            {
                new AITrendAnalysis { Topic = "Technology", TrendScore = 0.8 },
                new AITrendAnalysis { Topic = "Social Media", TrendScore = 0.7 },
                new AITrendAnalysis { Topic = "AI", TrendScore = 0.9 }
            },
            AudienceInsights = new List<AIAudienceInsight>
            {
                new AIAudienceInsight { Demographic = "Tech Professionals", EngagementRate = 0.75 }
            },
            ContentRecommendations = new List<AIContentRecommendation>
            {
                new AIContentRecommendation { ContentType = "Educational", PotentialReach = 1000 }
            }
        };

    private string BuildPerformancePredictionPrompt(Post post) =>
        $"Predict the performance of this social media post: {post.Content}";

    private AIPerformancePrediction ParsePerformancePrediction(string response, Post post) =>
        new AIPerformancePrediction
        {
            PredictedEngagement = 0.7,
            PredictedReach = 1500,
            ConfidenceLevel = 0.6,
            Factors = new List<string> { "Improved engagement" },
            PlatformPerformance = post.TargetPlatforms.ToDictionary(p => p, p => 0.7)
        };

    private string BuildVariationsPrompt(string content, int count) =>
        $"Create {count} variations of this content: {content}";

    private List<AIGeneratedContent> ParseVariations(string response, string originalContent, int count)
    {
        var variations = new List<AIGeneratedContent>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < Math.Min(count, lines.Length); i++)
        {
            variations.Add(new AIGeneratedContent
            {
                Content = lines[i].Trim(),
                CharacterCount = lines[i].Length,
                ConfidenceScore = 0.8 - (i * 0.1),
                Platform = SocialPlatform.X
            });
        }
        
        return variations;
    }

    private string BuildOptimalTimesPrompt(string userId) =>
        "Suggest optimal posting times for different social media platforms based on general best practices.";

    private Dictionary<SocialPlatform, DateTime> ParseOptimalTimes(string response)
    {
        var currentTime = DateTime.Now;
        return new Dictionary<SocialPlatform, DateTime>
        {
            [SocialPlatform.X] = currentTime.AddHours(2),
            [SocialPlatform.LinkedIn] = currentTime.AddHours(1),
            [SocialPlatform.Facebook] = currentTime.AddHours(3),
            [SocialPlatform.BlueSky] = currentTime.AddHours(4),
            [SocialPlatform.Threads] = currentTime.AddHours(2.5)
        };
    }

    private string BuildThreadPrompt(string content, int maxPosts) =>
        $"Convert this content into a {maxPosts}-post thread: {content}";

    private List<string> ParseThread(string response, int maxPosts)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Take(maxPosts).ToList();
    }

    private string BuildTranslationPrompt(string content, string language) =>
        $"Translate the following social media content to {language}, keeping the tone and style appropriate for social media:\n\n{content}";

    #endregion
}

/// <summary>
/// OpenAI-compatible chat response model for Foundry Local
/// </summary>
public class OpenAIChatResponse
{
    public List<ChatChoice>? Choices { get; set; }
    public ChatUsage? Usage { get; set; }
}

public class ChatChoice
{
    public ChatMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class ChatMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class ChatUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
} 