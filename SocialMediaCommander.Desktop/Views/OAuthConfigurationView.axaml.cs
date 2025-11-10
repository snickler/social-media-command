using Avalonia;
using Avalonia.Controls;
using SocialMediaCommander.Desktop.ViewModels;

namespace SocialMediaCommander.Desktop.Views;

public partial class OAuthConfigurationView : UserControl
{
    public OAuthConfigurationView()
    {
        InitializeComponent();

        // The DataContext will be set by the parent container or manually
        // The Design.DataContext in XAML is only for design-time preview
    }

    // Optional: Add a parameterized constructor for explicit ViewModel injection
    public OAuthConfigurationView(OAuthConfigurationViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}