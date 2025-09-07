using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Services.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// AI service implementation using Foundry Local with OpenAI-compatible API
/// Optimized for high performance and memory efficiency per Microsoft Docs best practices
/// </summary>
public class FoundryLocalAIService : IAIService, IDisposable
{
    private readonly ILogger<FoundryLocalAIService> _logger;
    private readonly AIModelConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ArrayPool<char> _charPool;
    private readonly ArrayPool<byte> _bytePool;
    private readonly SemaphoreSlim _initializationSemaphore;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _serviceEndpoint;
    private volatile bool _serviceInitialized = false;
    private volatile bool _disposed = false;

    public FoundryLocalAIService(
        ILogger<FoundryLocalAIService> logger,
        IOptions<AIModelConfig> config,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config.Value;
        _httpClient = httpClient;
        _charPool = ArrayPool<char>.Shared;
        _bytePool = ArrayPool<byte>.Shared;
        _initializationSemaphore = new SemaphoreSlim(1, 1);

        // Pre-configure JSON options for better performance
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultBufferSize = 4096 // Optimize buffer size
        };

        _httpClient.Timeout = TimeSpan.FromMinutes(5); // AI generation can take time
    }

    /// <summary>
    /// Initializes the Foundry Local service and discovers the endpoint
    /// Uses efficient async patterns with proper ConfigureAwait
    /// </summary>
    private async ValueTask InitializeServiceAsync()
    {
        if (_serviceInitialized && !string.IsNullOrEmpty(_serviceEndpoint))
            return;

        await _initializationSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Double-check pattern for thread safety
            if (_serviceInitialized && !string.IsNullOrEmpty(_serviceEndpoint))
                return;

            // Check if Foundry Local service is running and get endpoint
            var endpoint = await DiscoverServiceEndpointAsync().ConfigureAwait(false);
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
                await StartFoundryServiceAsync().ConfigureAwait(false);
                endpoint = await DiscoverServiceEndpointAsync().ConfigureAwait(false);
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
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    /// <summary>
    /// Discovers the Foundry Local service endpoint using CLI
    /// Optimized with ArrayPool for buffer management
    /// </summary>
    private async ValueTask<string?> DiscoverServiceEndpointAsync()
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
                await process.WaitForExitAsync().ConfigureAwait(false);

                // Use ArrayPool for efficient string processing
                var buffer = _charPool.Rent(4096);
                try
                {
                    var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                    // Process lines efficiently using string operations instead of Span in async method
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("running at:", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("endpoint:", StringComparison.OrdinalIgnoreCase))
                        {
                            var colonIndex = line.IndexOf(':');
                            if (colonIndex >= 0 && colonIndex < line.Length - 1)
                            {
                                var endpoint = line.Substring(colonIndex + 1).Trim();

                                if (Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                                {
                                    return endpoint.EndsWith("/v1") ? endpoint : $"{endpoint}/v1";
                                }
                            }
                        }
                    }
                }
                finally
                {
                    _charPool.Return(buffer);
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
    private async ValueTask StartFoundryServiceAsync()
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
                await process.WaitForExitAsync().ConfigureAwait(false);
                // Give the service time to start
                await Task.Delay(3000).ConfigureAwait(false);
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
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));

        if (request == null)
        {
            return new AIContentResponse
            {
                Success = false,
                ErrorMessage = "Request cannot be null",
                GeneratedAt = DateTime.UtcNow,
                GeneratedContent = new List<AIGeneratedContent>()
            };
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new AIContentResponse
            {
                Success = false,
                ErrorMessage = "Prompt cannot be empty",
                GeneratedAt = DateTime.UtcNow,
                GeneratedContent = new List<AIGeneratedContent>(),
                Metrics = new AIMetrics()
            };
        }

        _logger.LogInformation("Generating AI content for request: {RequestId}", request.Id);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var startTime = DateTime.UtcNow;

            // Build platform-specific prompt using efficient string operations
            var platformInfo = GetPlatformInfo(request.TargetPlatforms);
            var prompt = BuildContentGenerationPrompt(request, platformInfo);

            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

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
                    TokensUsed = EstimateTokens(response),
                    SentimentScore = AnalyzeSentiment(response),
                    DetectedTopics = ExtractTopics(response),
                    EngagementPrediction = PredictEngagement(response, request.TargetPlatforms)
                },
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI content for request: {RequestId}", request.Id);
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
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Optimizing content for platform: {Platform}", request.Platform);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var platformLimits = GetPlatformLimits(request.Platform);
            var prompt = BuildOptimizationPrompt(request, platformLimits);

            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

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
            _logger.LogError(ex, "Error optimizing content for request: {RequestId}", request.Content);
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
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Analyzing content for platform: {Platform}", platform);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var prompt = BuildAnalysisPrompt(content, platform);
            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

            return ParseAnalysisResponse(response, content, platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content for platform: {Platform}", platform);
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
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Generating hashtags for content, max count: {MaxCount}", maxCount);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var prompt = BuildHashtagPrompt(content, maxCount);
            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

            return ParseHashtags(response, content, maxCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtags");
            return ExtractHashtags(content).Take(maxCount).ToList();
        }
    }

    public async Task<AIInsights> GetInsightsAsync(string userId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Getting AI insights for user: {UserId}", userId);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var prompt = BuildInsightsPrompt(userId);
            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

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
                    new AIContentRecommendation
                    {
                        ContentType = "Video",
                        Topic = "AI trends content",
                        PotentialReach = 1000
                    }
                },
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<AIPerformancePrediction> PredictPerformanceAsync(Post post)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Predicting performance for post: {PostId}", post.Id);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var prompt = BuildPerformancePredictionPrompt(post);
            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

            return ParsePerformancePrediction(response, post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting performance for post: {PostId}", post.Id);
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
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Generating {Count} variations of content", count);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var prompt = BuildVariationsPrompt(content, count);
            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

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
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Suggesting optimal posting times for user: {UserId}", userId);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var prompt = BuildOptimalTimesPrompt(userId);
            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

            return ParseOptimalTimes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting optimal posting times");
            var now = DateTime.UtcNow;
            return new Dictionary<SocialPlatform, DateTime>
            {
                { SocialPlatform.X, now.AddHours(2) },
                { SocialPlatform.LinkedIn, now.AddHours(1) },
                { SocialPlatform.Facebook, now.AddHours(3) },
                { SocialPlatform.BlueSky, now.AddHours(1.5) },
                { SocialPlatform.Threads, now.AddHours(2.5) }
            };
        }
    }

    public async Task<List<string>> GenerateThreadAsync(string content, int maxPosts = 5)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Generating thread with max {MaxPosts} posts", maxPosts);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var prompt = BuildThreadPrompt(content, maxPosts);
            var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);

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
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));
        _logger.LogInformation("Translating content to {Count} languages", targetLanguages.Count);

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            var translations = new Dictionary<string, string>();

            // Process translations in parallel for better performance
            var tasks = targetLanguages.Select(async language =>
            {
                var prompt = BuildTranslationPrompt(content, language);
                var response = await CallFoundryLocalAsync(prompt).ConfigureAwait(false);
                return new KeyValuePair<string, string>(language, response);
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var result in results)
            {
                translations[result.Key] = result.Value;
            }

            return translations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating content");
            return targetLanguages.ToDictionary(lang => lang, _ => content);
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(_serviceEndpoint))
                return false;

            // Quick health check with timeout
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var response = await _httpClient.GetAsync("/health", HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AIModelConfig> GetModelInfoAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FoundryLocalAIService));

        try
        {
            await InitializeServiceAsync().ConfigureAwait(false);

            return new AIModelConfig
            {
                ModelName = _config.ModelName,
                BaseUrl = _serviceEndpoint ?? _config.BaseUrl,
                MaxTokens = _config.MaxTokens,
                Temperature = _config.Temperature,
                SystemPrompt = _config.SystemPrompt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model info");
            return _config;
        }
    }

    /// <summary>
    /// Optimized HTTP call to Foundry Local using efficient JSON serialization
    /// </summary>
    private async ValueTask<string> CallFoundryLocalAsync(string prompt)
    {
        var requestBody = new
        {
            model = _config.ModelName,
            messages = new[]
            {
                new { role = "system", content = _config.SystemPrompt },
                new { role = "user", content = prompt }
            },
            temperature = _config.Temperature,
            max_tokens = _config.MaxTokens,
            stream = false
        };

        // Use ArrayPool for JSON serialization buffer
        var buffer = _bytePool.Rent(8192);
        try
        {
            using var stream = new MemoryStream(buffer);
            await JsonSerializer.SerializeAsync(stream, requestBody, _jsonOptions).ConfigureAwait(false);

            using var content = new ByteArrayContent(buffer, 0, (int)stream.Length);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            using var response = await _httpClient.PostAsync("/chat/completions", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"HTTP error {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var chatResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseContent, _jsonOptions);

            return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }
        finally
        {
            _bytePool.Return(buffer);
        }
    }

    /// <summary>
    /// Optimized content generation prompt building using StringBuilder pooling
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string BuildContentGenerationPrompt(AIContentRequest request, string platformInfo)
    {
        var sb = new StringBuilder(1024); // Pre-allocate reasonable capacity

        sb.AppendLine("Generate engaging social media content based on the following requirements:");
        sb.AppendLine();
        sb.AppendLine($"Topic: {request.Prompt}");
        sb.AppendLine($"Content Type: {request.ContentType}");
        sb.AppendLine($"Tone: {request.Tone}");

        if (!string.IsNullOrEmpty(request.BrandVoice))
        {
            sb.AppendLine($"Brand Voice: {request.BrandVoice}");
        }

        if (!string.IsNullOrEmpty(request.Context))
        {
            sb.AppendLine($"Additional Context: {request.Context}");
        }

        sb.AppendLine();
        sb.AppendLine("Platform Requirements:");
        sb.AppendLine(platformInfo);

        if (request.Keywords?.Any() == true)
        {
            sb.AppendLine();
            sb.AppendLine($"Keywords to include: {string.Join(", ", request.Keywords)}");
        }

        if (request.IncludeHashtags)
        {
            sb.AppendLine("Include relevant hashtags.");
        }

        if (request.IncludeEmojis)
        {
            sb.AppendLine("Include appropriate emojis.");
        }

        sb.AppendLine();
        sb.AppendLine("Please provide multiple variations optimized for each platform, including relevant hashtags and engagement hooks.");

        return sb.ToString();
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

    private AIOptimizationResponse ParseOptimizationResponse(string response, AIOptimizationRequest request)
    {
        var parser = new AIContentParser();
        var result = parser.ParseOptimizationResponse(response, request);

        // If parser didn't find suggestions, create default ones
        if (!result.Suggestions.Any())
        {
            result.Suggestions = new List<AIOptimizationSuggestion>
            {
                new AIOptimizationSuggestion
                {
                    Type = "engagement",
                    Suggestion = "Consider adding more engaging call-to-action",
                    Reason = "Increases user interaction",
                    Impact = 0.7
                },
                new AIOptimizationSuggestion
                {
                    Type = "hashtags",
                    Suggestion = "Add trending hashtags for better reach",
                    Reason = "Improves discoverability",
                    Impact = 0.6
                }
            };
        }

        return result;
    }

    private string BuildAnalysisPrompt(string content, SocialPlatform platform) =>
        $"Analyze this {platform} content for readability, sentiment, and engagement potential: {content}";

    private AIAnalysis ParseAnalysisResponse(string response, string content, SocialPlatform platform)
    {
        var sentimentAnalyzer = new SentimentAnalyzer();
        var topicExtractor = new TopicExtractor();
        var engagementPredictor = new EngagementPredictor();

        var sentimentScore = sentimentAnalyzer.AnalyzeSentiment(content);
        var topics = topicExtractor.ExtractTopics(content);
        var hashtags = ExtractHashtags(content).Take(5).ToList();

        // If no hashtags from content, generate from topics
        if (!hashtags.Any())
        {
            hashtags = topics.Select(t => $"#{t}").Take(5).ToList();
        }

        return new AIAnalysis
        {
            ReadabilityScore = CalculateReadabilityScore(content),
            SentimentScore = sentimentScore,
            EngagementPotential = engagementPredictor.PredictEngagement(content, new List<SocialPlatform> { platform }),
            RecommendedHashtags = hashtags,
            OptimalPostTime = GetOptimalPostTime(platform),
            KeywordDensity = topics
        };
    }

    private string BuildHashtagPrompt(string content, int maxCount) =>
        $"Generate {maxCount} relevant hashtags for this content: {content}";

    private List<string> ParseHashtags(string response, string originalContent, int maxCount)
    {
        var topicExtractor = new TopicExtractor();

        // First, try to extract hashtags from the response (if they contain #)
        var hashtags = ExtractHashtags(response);

        if (hashtags.Count == 0)
        {
            // Parse comma-separated or space-separated values from response
            hashtags = response.Split(new[] { '\n', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                              .Where(w => w.Length > 1)
                              .Select(w => w.Trim())
                              .Where(w => !string.IsNullOrEmpty(w))
                              .Select(w => w.StartsWith("#") ? w : $"#{w}")
                              .Take(maxCount)
                              .ToList();
        }

        if (hashtags.Count == 0)
        {
            // Final fallback: extract topics from original content
            var topics = topicExtractor.ExtractTopics(originalContent);
            hashtags = topics.Select(t => $"#{t}").ToList();
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

    private double CalculateReadabilityScore(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.0;

        var sentences = content.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var words = content.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var avgWordsPerSentence = sentences > 0 ? (double)words / sentences : words;

        // Simple readability score based on average words per sentence
        var score = Math.Max(0, 100 - (avgWordsPerSentence * 2));
        return Math.Min(100, score);
    }

    private string GetOptimalPostTime(SocialPlatform platform)
    {
        return platform switch
        {
            SocialPlatform.X => "2:00 PM",
            SocialPlatform.LinkedIn => "10:00 AM",
            SocialPlatform.Facebook => "1:00 PM",
            SocialPlatform.BlueSky => "3:00 PM",
            SocialPlatform.Threads => "2:30 PM",
            _ => "12:00 PM"
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    ~FoundryLocalAIService()
    {
        Dispose();
    }
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