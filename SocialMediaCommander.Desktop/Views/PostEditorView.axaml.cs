using Avalonia.Controls;
using SocialMediaCommander.Desktop.ViewModels;

namespace SocialMediaCommander.Desktop.Views;

public partial class PostEditorView : UserControl
{
    public PostEditorView()
    {
        InitializeComponent();
    }
    
    public PostEditorView(PostEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
} 