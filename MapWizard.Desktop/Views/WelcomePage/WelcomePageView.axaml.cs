using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.WelcomePage;

public partial class WelcomePageView : UserControl
{
    public WelcomePageView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is WelcomePageViewModel vm)
            {
                vm.RefreshSongsFolderStatus();
            }
        };
    }

    private void OpenHitSoundCopier_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToHitSoundCopier();
    }

    private void OpenMetadataManager_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToMetadataManager();
    }

    private void OpenHitSoundVisualizer_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToHitSoundVisualizer();
    }

    private void OpenComboColourStudio_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToComboColourStudio();
    }

    private void OpenMapCleaner_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToMapCleaner();
    }

    private void OpenSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToSettings();
    }
}
