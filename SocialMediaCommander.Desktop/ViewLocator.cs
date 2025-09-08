using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Desktop.Views;

namespace SocialMediaCommander.Desktop;

public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> _viewMappings = new()
    {
        { typeof(MainWindowViewModel), () => new MainWindow() },
        { typeof(PostEditorViewModel), () => new PostEditorView() },
        { typeof(SocialFeedViewModel), () => new SocialFeedView() },
        { typeof(AccountManagerViewModel), () => new AccountManagerView() },
        { typeof(AnalyticsDashboardViewModel), () => new AnalyticsDashboardView() },
        { typeof(SettingsViewModel), () => new SettingsView() },
        { typeof(SchedulerViewModel), () => new SchedulerView() },
        { typeof(OAuthConfigurationViewModel), () => new OAuthConfigurationView() },
        { typeof(DocumentationViewModel), () => new DocumentationView() }
    };

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var paramType = param.GetType();
        
        if (_viewMappings.TryGetValue(paramType, out var factory))
        {
            return factory();
        }

        return new TextBlock { Text = "Not Found: " + paramType.Name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
