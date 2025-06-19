using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Desktop.Services;

namespace SocialMediaCommander.Desktop;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSocialMediaCommanderServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AIModelConfig>(configuration.GetSection("AI"));
        
        // Core Services
        services.AddHttpClient<IAIService, FoundryLocalAIService>();
        services.AddSingleton<IOAuthConfigurationService, OAuthConfigurationService>();
        services.AddHttpClient<IAuthenticationService, OAuthAuthenticationService>();
        
        // Platform Services
        services.AddScoped<IBlueSkyService, BlueSkyService>();
        services.AddScoped<ITwitterService, TwitterService>();
        services.AddScoped<ILinkedInService, LinkedInService>();
        services.AddScoped<IThreadsService, ThreadsService>();
        services.AddScoped<IFacebookService, FacebookService>();
        
        // Application Services
        services.AddScoped<IPostService, MockPostService>();
        services.AddScoped<IAccountService, InMemoryAccountService>();
        services.AddScoped<IMediaService, MockMediaService>();
        services.AddScoped<IFeedService, InMemoryFeedService>();
        
        // Enhanced Services
        services.AddScoped<IAIService, FoundryLocalAIService>();
        
        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<PostEditorViewModel>();
        services.AddTransient<SocialFeedViewModel>();
        services.AddTransient<AccountManagerViewModel>();
        services.AddTransient<AnalyticsDashboardViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SchedulerViewModel>();
        services.AddTransient<AIAssistantViewModel>();
        services.AddTransient<OAuthConfigurationViewModel>();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        
        return services;
    }
} 