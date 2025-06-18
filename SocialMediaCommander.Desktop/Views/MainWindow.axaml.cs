using Avalonia.Controls;
using System;
using SocialMediaCommander.Desktop.ViewModels;

namespace SocialMediaCommander.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Console.WriteLine("MainWindow constructor called");
        
        // Debug: Add event handlers to detect button clicks
        this.Loaded += (s, e) =>
        {
            Console.WriteLine("MainWindow loaded event fired");
            Console.WriteLine($"DataContext type: {DataContext?.GetType().Name}");
            
            if (DataContext is MainWindowViewModel vm)
            {
                Console.WriteLine("DataContext successfully cast to MainWindowViewModel");
                Console.WriteLine($"Commands available: StandardView={vm.SetStandardViewCommand != null}, CompactView={vm.SetCompactViewCommand != null}");
            }
            else
            {
                Console.WriteLine("Failed to cast DataContext to MainWindowViewModel");
            }
        };
    }
    
    private void StandardViewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Console.WriteLine("StandardView button clicked!");
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetStandardViewCommand?.Execute(null);
        }
    }
    
    private void CompactViewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Console.WriteLine("CompactView button clicked!");
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetCompactViewCommand?.Execute(null);
        }
    }
    
    private void ManageAccountsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Console.WriteLine("ManageAccounts button clicked!");
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ManageAccountsCommand?.Execute(null);
        }
    }
    
    private void SingleThreadModeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Console.WriteLine("SingleThreadMode button clicked!");
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetSingleThreadModeCommand?.Execute(null);
        }
    }
    
    private void ThreadsOnlyModeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Console.WriteLine("ThreadsOnlyMode button clicked!");
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetThreadsOnlyModeCommand?.Execute(null);
        }
    }
    
    private void SplitViewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Console.WriteLine("SplitView button clicked!");
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetSplitViewCommand?.Execute(null);
        }
    }
    
    private void ComposeOnlyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Console.WriteLine("ComposeOnly button clicked!");
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetComposeOnlyCommand?.Execute(null);
        }
    }
}