using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace MapWizard.Desktop.Views.WelcomePage;

public partial class WelcomePageView : UserControl
{
    public WelcomePageView()
    {
        InitializeComponent();
    }

    private void OpenHitSoundCopier_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToHitSoundCopier();
    }

    private void OpenMetadataManager_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToMetadataManager();
    }

    private void OpenComboColourStudio_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToComboColourStudio();
    }
}
