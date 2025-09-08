using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for AIAssistantViewModel
/// Tests MVVM compliance, command execution, AI integration, and property notifications
/// </summary>
public class AIAssistantViewModelTests
{
    private readonly Mock<IAIService> _mockAIService;
    private readonly Mock<ILogger<AIAssistantViewModel>> _mockLogger;
    private readonly AIAssistantViewModel _viewModel;

    public AIAssistantViewModelTests()
    {
        _mockAIService = new Mock<IAIService>();
        _mockLogger = new Mock<ILogger<AIAssistantViewModel>>();
        _viewModel = new AIAssistantViewModel(_mockAIService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Assert
        Assert.Equal(string.Empty, _viewModel.Prompt);
        Assert.Equal(string.Empty, _viewModel.GeneratedContent);
        Assert.False(_viewModel.IsGenerating);
        Assert.Equal(AITone.Professional, _viewModel.SelectedTone);
        Assert.Equal(AIContentType.Post, _viewModel.SelectedContentType);
        Assert.True(_viewModel.IncludeHashtags);
        Assert.False(_viewModel.IncludeEmojis);
        Assert.Equal(string.Empty, _viewModel.BrandVoice);
        Assert.Equal(string.Empty, _viewModel.Keywords);
    }

    [Fact]
    public void Constructor_ShouldInitializeCollectionsCorrectly()
    {
        // Assert
        Assert.NotNull(_viewModel.TargetPlatforms);
        Assert.Equal(5, _viewModel.TargetPlatforms.Count);
        Assert.True(_viewModel.TargetPlatforms.First(p => p.Platform == SocialPlatform.X).IsSelected);
        Assert.False(_viewModel.TargetPlatforms.First(p => p.Platform == SocialPlatform.LinkedIn).IsSelected);

        Assert.NotNull(_viewModel.ContentVariations);
        Assert.Empty(_viewModel.ContentVariations);

        Assert.NotNull(_viewModel.GeneratedHashtags);
        Assert.Empty(_viewModel.GeneratedHashtags);

        Assert.NotNull(_viewModel.ThreadPosts);
        Assert.Empty(_viewModel.ThreadPosts);

        Assert.NotNull(_viewModel.OptimizationSuggestions);
        Assert.Empty(_viewModel.OptimizationSuggestions);
    }

    [Fact]
    public void Constructor_ShouldInitializeCommandsCorrectly()
    {
        // Assert
        Assert.NotNull(_viewModel.GenerateContentCommand);
        Assert.NotNull(_viewModel.OptimizeContentCommand);
        Assert.NotNull(_viewModel.GenerateVariationsCommand);
        Assert.NotNull(_viewModel.GenerateHashtagsCommand);
        Assert.NotNull(_viewModel.GenerateThreadCommand);
    }

    [Fact]
    public void Constructor_ShouldInitializeEnumPropertiesCorrectly()
    {
        // Assert
        Assert.NotNull(_viewModel.AITones);
        Assert.True(_viewModel.AITones.Length > 0);

        Assert.NotNull(_viewModel.AIContentTypes);
        Assert.True(_viewModel.AIContentTypes.Length > 0);
    }

    [Fact]
    public void Prompt_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.Prompt))
                eventRaised = true;
        };

        // Act
        _viewModel.Prompt = "Test prompt";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Test prompt", _viewModel.Prompt);
    }

    [Fact]
    public void GeneratedContent_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.GeneratedContent))
                eventRaised = true;
        };

        // Act
        _viewModel.GeneratedContent = "Generated content";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Generated content", _viewModel.GeneratedContent);
    }

    [Fact]
    public void IsGenerating_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.IsGenerating))
                eventRaised = true;
        };

        // Act
        _viewModel.IsGenerating = true;

        // Assert
        Assert.True(eventRaised);
        Assert.True(_viewModel.IsGenerating);
    }

    [Theory]
    [InlineData(AITone.Professional)]
    [InlineData(AITone.Casual)]
    [InlineData(AITone.Formal)]
    [InlineData(AITone.Friendly)]
    public void SelectedTone_Set_ShouldUpdateValueAndRaisePropertyChanged(AITone tone)
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.SelectedTone))
                eventRaised = true;
        };

        // Act
        _viewModel.SelectedTone = tone;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(tone, _viewModel.SelectedTone);
    }

    [Theory]
    [InlineData(AIContentType.Post)]
    [InlineData(AIContentType.Thread)]
    [InlineData(AIContentType.Story)]
    [InlineData(AIContentType.Advertisement)]
    public void SelectedContentType_Set_ShouldUpdateValueAndRaisePropertyChanged(AIContentType contentType)
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.SelectedContentType))
                eventRaised = true;
        };

        // Act
        _viewModel.SelectedContentType = contentType;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(contentType, _viewModel.SelectedContentType);
    }

    [Fact]
    public void IncludeHashtags_Set_ShouldUpdateValueAndRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.IncludeHashtags))
                eventRaised = true;
        };

        // Act
        _viewModel.IncludeHashtags = false;

        // Assert
        Assert.True(eventRaised);
        Assert.False(_viewModel.IncludeHashtags);
    }

    [Fact]
    public void IncludeEmojis_Set_ShouldUpdateValueAndRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.IncludeEmojis))
                eventRaised = true;
        };

        // Act
        _viewModel.IncludeEmojis = true;

        // Assert
        Assert.True(eventRaised);
        Assert.True(_viewModel.IncludeEmojis);
    }

    [Fact]
    public void BrandVoice_Set_ShouldUpdateValueAndRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.BrandVoice))
                eventRaised = true;
        };

        // Act
        _viewModel.BrandVoice = "Professional and engaging";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Professional and engaging", _viewModel.BrandVoice);
    }

    [Fact]
    public void Keywords_Set_ShouldUpdateValueAndRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.Keywords))
                eventRaised = true;
        };

        // Act
        _viewModel.Keywords = "technology, innovation, AI";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("technology, innovation, AI", _viewModel.Keywords);
    }

    [Fact]
    public void GenerateContentCommand_CanExecute_ShouldReturnFalseWhenPromptEmpty()
    {
        // Arrange
        _viewModel.Prompt = string.Empty;
        _viewModel.IsGenerating = false;

        // Act & Assert
        Assert.False(_viewModel.GenerateContentCommand.CanExecute(null));
    }

    [Fact]
    public void GenerateContentCommand_CanExecute_ShouldReturnFalseWhenGenerating()
    {
        // Arrange
        _viewModel.Prompt = "Test prompt";
        _viewModel.IsGenerating = true;

        // Act & Assert
        Assert.False(_viewModel.GenerateContentCommand.CanExecute(null));
    }

    [Fact]
    public void GenerateContentCommand_CanExecute_ShouldReturnTrueWhenValidState()
    {
        // Arrange
        _viewModel.Prompt = "Test prompt";
        _viewModel.IsGenerating = false;

        // Act & Assert
        Assert.True(_viewModel.GenerateContentCommand.CanExecute(null));
    }

    [Fact]
    public async Task GenerateContentCommand_Execute_ShouldCallAIServiceWithCorrectRequest()
    {
        // Arrange
        var expectedResponse = new AIContentResponse
        {
            Success = true,
            GeneratedContent = new List<AIGeneratedContent>
            {
                new() { Content = "Generated test content", Platform = SocialPlatform.X }
            }
        };

        _mockAIService.Setup(x => x.GenerateContentAsync(It.IsAny<AIContentRequest>()))
                     .ReturnsAsync(expectedResponse);

        _viewModel.Prompt = "Test prompt";
        _viewModel.SelectedTone = AITone.Casual;
        _viewModel.SelectedContentType = AIContentType.Thread;
        _viewModel.IncludeHashtags = true;
        _viewModel.IncludeEmojis = false;
        _viewModel.BrandVoice = "Tech company";
        _viewModel.Keywords = "AI, technology, innovation";

        // Act
        await _viewModel.GenerateContentCommand.ExecuteAsync(null);

        // Assert
        _mockAIService.Verify(x => x.GenerateContentAsync(It.Is<AIContentRequest>(r =>
            r.Prompt == "Test prompt" &&
            r.Tone == AITone.Casual &&
            r.ContentType == AIContentType.Thread &&
            r.IncludeHashtags == true &&
            r.IncludeEmojis == false &&
            r.BrandVoice == "Tech company" &&
            r.Keywords.Count == 3 &&
            r.Keywords.Contains("AI") &&
            r.TargetPlatforms.Contains(SocialPlatform.X)
        )), Times.Once);

        Assert.Equal("Generated test content", _viewModel.GeneratedContent);
        Assert.Single(_viewModel.ContentVariations);
    }

    [Fact]
    public async Task GenerateContentCommand_Execute_ShouldHandleAIServiceFailure()
    {
        // Arrange
        var failureResponse = new AIContentResponse
        {
            Success = false,
            ErrorMessage = "AI service error"
        };

        _mockAIService.Setup(x => x.GenerateContentAsync(It.IsAny<AIContentRequest>()))
                     .ReturnsAsync(failureResponse);

        _viewModel.Prompt = "Test prompt";

        // Act
        await _viewModel.GenerateContentCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("Error: AI service error", _viewModel.GeneratedContent);
    }

    [Fact]
    public async Task GenerateContentCommand_Execute_ShouldHandleException()
    {
        // Arrange
        _mockAIService.Setup(x => x.GenerateContentAsync(It.IsAny<AIContentRequest>()))
                     .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        _viewModel.Prompt = "Test prompt";

        // Act
        await _viewModel.GenerateContentCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("Error: Service unavailable", _viewModel.GeneratedContent);
    }

    [Fact]
    public async Task GenerateContentCommand_Execute_ShouldSetIsGeneratingDuringOperation()
    {
        // Arrange
        var taskCompletionSource = new TaskCompletionSource<AIContentResponse>();
        _mockAIService.Setup(x => x.GenerateContentAsync(It.IsAny<AIContentRequest>()))
                     .Returns(taskCompletionSource.Task);

        _viewModel.Prompt = "Test prompt";

        // Act
        var executeTask = _viewModel.GenerateContentCommand.ExecuteAsync(null);

        // Assert - Should be generating
        Assert.True(_viewModel.IsGenerating);

        // Complete the operation
        taskCompletionSource.SetResult(new AIContentResponse { Success = true, GeneratedContent = new List<AIGeneratedContent>() });
        await executeTask;

        // Assert - Should no longer be generating
        Assert.False(_viewModel.IsGenerating);
    }

    [Fact]
    public void OptimizeContentCommand_CanExecute_ShouldReturnFalseWhenContentEmpty()
    {
        // Arrange
        _viewModel.GeneratedContent = string.Empty;
        _viewModel.IsGenerating = false;

        // Act & Assert
        Assert.False(_viewModel.OptimizeContentCommand.CanExecute(null));
    }

    [Fact]
    public async Task OptimizeContentCommand_Execute_ShouldCallAIServiceCorrectly()
    {
        // Arrange
        var expectedResponse = new AIOptimizationResponse
        {
            OptimizedContent = "Optimized content",
            Suggestions = new List<AIOptimizationSuggestion>
            {
                new() { Type = "Engagement", Suggestion = "Add more engagement", Reason = "Better interaction", Impact = 0.8 }
            }
        };

        _mockAIService.Setup(x => x.OptimizeContentAsync(It.IsAny<AIOptimizationRequest>()))
                     .ReturnsAsync(expectedResponse);

        _viewModel.GeneratedContent = "Original content";

        // Act
        await _viewModel.OptimizeContentCommand.ExecuteAsync(null);

        // Assert
        _mockAIService.Verify(x => x.OptimizeContentAsync(It.Is<AIOptimizationRequest>(r =>
            r.Content == "Original content" &&
            r.Platform == SocialPlatform.X &&
            r.OptimizationType == AIOptimizationType.Engagement
        )), Times.Once);

        Assert.Equal("Optimized content", _viewModel.GeneratedContent);
        Assert.Single(_viewModel.OptimizationSuggestions);
    }

    [Fact]
    public async Task GenerateVariationsCommand_Execute_ShouldCallAIServiceCorrectly()
    {
        // Arrange
        var expectedVariations = new List<AIGeneratedContent>
        {
            new() { Content = "Variation 1", Platform = SocialPlatform.X },
            new() { Content = "Variation 2", Platform = SocialPlatform.X }
        };

        _mockAIService.Setup(x => x.GenerateVariationsAsync("Original content", 3))
                     .ReturnsAsync(expectedVariations);

        _viewModel.GeneratedContent = "Original content";

        // Act
        await _viewModel.GenerateVariationsCommand.ExecuteAsync(null);

        // Assert
        _mockAIService.Verify(x => x.GenerateVariationsAsync("Original content", 3), Times.Once);
        Assert.Equal(2, _viewModel.ContentVariations.Count);
        Assert.Contains(_viewModel.ContentVariations, v => v.Content == "Variation 1");
        Assert.Contains(_viewModel.ContentVariations, v => v.Content == "Variation 2");
    }

    [Fact]
    public async Task GenerateHashtagsCommand_Execute_ShouldCallAIServiceCorrectly()
    {
        // Arrange
        var expectedHashtags = new List<string> { "technology", "innovation", "AI" };

        _mockAIService.Setup(x => x.GenerateHashtagsAsync("Test content", 10))
                     .ReturnsAsync(expectedHashtags);

        _viewModel.GeneratedContent = "Test content";

        // Act
        await _viewModel.GenerateHashtagsCommand.ExecuteAsync(null);

        // Assert
        _mockAIService.Verify(x => x.GenerateHashtagsAsync("Test content", 10), Times.Once);
        Assert.Equal(3, _viewModel.GeneratedHashtags.Count);
        Assert.Contains("#technology", _viewModel.GeneratedHashtags);
        Assert.Contains("#innovation", _viewModel.GeneratedHashtags);
        Assert.Contains("#AI", _viewModel.GeneratedHashtags);
    }

    [Fact]
    public async Task GenerateThreadCommand_Execute_ShouldCallAIServiceCorrectly()
    {
        // Arrange
        var expectedThreadPosts = new List<string> { "Post 1", "Post 2", "Post 3" };

        _mockAIService.Setup(x => x.GenerateThreadAsync("Thread content", 5))
                     .ReturnsAsync(expectedThreadPosts);

        _viewModel.GeneratedContent = "Thread content";

        // Act
        await _viewModel.GenerateThreadCommand.ExecuteAsync(null);

        // Assert
        _mockAIService.Verify(x => x.GenerateThreadAsync("Thread content", 5), Times.Once);
        Assert.Equal(3, _viewModel.ThreadPosts.Count);
        Assert.Contains("Post 1", _viewModel.ThreadPosts);
        Assert.Contains("Post 2", _viewModel.ThreadPosts);
        Assert.Contains("Post 3", _viewModel.ThreadPosts);
    }

    [Fact]
    public async Task AllAsyncCommands_ShouldHandleExceptionsGracefully()
    {
        // Arrange
        _mockAIService.Setup(x => x.OptimizeContentAsync(It.IsAny<AIOptimizationRequest>()))
                     .ThrowsAsync(new InvalidOperationException("Test exception"));
        _mockAIService.Setup(x => x.GenerateVariationsAsync(It.IsAny<string>(), It.IsAny<int>()))
                     .ThrowsAsync(new InvalidOperationException("Test exception"));
        _mockAIService.Setup(x => x.GenerateHashtagsAsync(It.IsAny<string>(), It.IsAny<int>()))
                     .ThrowsAsync(new InvalidOperationException("Test exception"));
        _mockAIService.Setup(x => x.GenerateThreadAsync(It.IsAny<string>(), It.IsAny<int>()))
                     .ThrowsAsync(new InvalidOperationException("Test exception"));

        _viewModel.GeneratedContent = "Test content";

        // Act & Assert - All commands should complete without throwing
        await _viewModel.OptimizeContentCommand.ExecuteAsync(null);
        await _viewModel.GenerateVariationsCommand.ExecuteAsync(null);
        await _viewModel.GenerateHashtagsCommand.ExecuteAsync(null);
        await _viewModel.GenerateThreadCommand.ExecuteAsync(null);

        // Verify IsGenerating is reset after each operation
        Assert.False(_viewModel.IsGenerating);
    }

    [Fact]
    public void PropertyChanged_ShouldUpdateCommandCanExecuteStates()
    {
        // Arrange
        var initialCanExecuteGenerate = _viewModel.GenerateContentCommand.CanExecute(null);
        var initialCanExecuteOptimize = _viewModel.OptimizeContentCommand.CanExecute(null);

        // Act
        _viewModel.Prompt = "Test prompt";
        _viewModel.GeneratedContent = "Generated content";

        // Assert
        Assert.False(initialCanExecuteGenerate);
        Assert.False(initialCanExecuteOptimize);
        Assert.True(_viewModel.GenerateContentCommand.CanExecute(null));
        Assert.True(_viewModel.OptimizeContentCommand.CanExecute(null));
    }
}

/// <summary>
/// Unit tests for PlatformSelectionItem helper class
/// </summary>
public class PlatformSelectionItemTests
{
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var item = new PlatformSelectionItem
        {
            Platform = SocialPlatform.LinkedIn,
            IsSelected = true
        };

        // Assert
        Assert.Equal(SocialPlatform.LinkedIn, item.Platform);
        Assert.True(item.IsSelected);
        Assert.Equal("LinkedIn", item.DisplayName);
    }

    [Fact]
    public void IsSelected_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var item = new PlatformSelectionItem();
        var eventRaised = false;

        item.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(item.IsSelected))
                eventRaised = true;
        };

        // Act
        item.IsSelected = true;

        // Assert
        Assert.True(eventRaised);
        Assert.True(item.IsSelected);
    }

    [Theory]
    [InlineData(SocialPlatform.X, "X")]
    [InlineData(SocialPlatform.LinkedIn, "LinkedIn")]
    [InlineData(SocialPlatform.Facebook, "Facebook")]
    [InlineData(SocialPlatform.BlueSky, "BlueSky")]
    [InlineData(SocialPlatform.Threads, "Threads")]
    public void DisplayName_ShouldReturnCorrectPlatformName(SocialPlatform platform, string expectedName)
    {
        // Arrange
        var item = new PlatformSelectionItem { Platform = platform };

        // Act & Assert
        Assert.Equal(expectedName, item.DisplayName);
    }
}