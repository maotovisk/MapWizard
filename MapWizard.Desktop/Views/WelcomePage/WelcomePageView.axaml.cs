using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MapWizard.Desktop.Models;
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

    private void OpenTool_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: QuickStartTool tool })
        {
            return;
        }

        var mainWindow = this.FindAncestorOfType<MainWindow>();
        switch (tool.Key)
        {
            case "copier":
                mainWindow?.NavigateToHitSoundCopier();
                break;
            case "metadata":
                mainWindow?.NavigateToMetadataManager();
                break;
            case "hitsound-editor":
                mainWindow?.NavigateToHitSoundVisualizer();
                break;
            case "combo-colour":
                mainWindow?.NavigateToComboColourStudio();
                break;
            case "map-cleaner":
                mainWindow?.NavigateToMapCleaner();
                break;
        }
    }

    private void OpenSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindAncestorOfType<MainWindow>()?.NavigateToSettings();
    }
}
