using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MapWizard.Desktop.ViewModels;
using Material.Styles.Controls;
using MsBox.Avalonia;
using Velopack;
using Velopack.Sources;

namespace MapWizard.Desktop.Views;

public partial class MainWindow : Window
{
    string snackbarName { get; set; } = "SnackbarMainWindow";
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
    
    private static async Task UpdateMyApp()
    {
        var mgr = new UpdateManager(new GithubSource("https://github.com/maotovisk/MapWizard", null, false, null));
        
        if (!mgr.IsInstalled)
            return; // app is not installed
        
        SnackbarHost.Post("Checking for updates...", "SnackbarMainWindow", DispatcherPriority.Normal);
        
        var newVersion = await mgr.CheckForUpdatesAsync();
        if (newVersion == null)
            return;
        
        SnackbarHost.Post($"Update available, downloading version {newVersion.BaseRelease?.Version}...", "snackbarName", DispatcherPriority.Normal);
        
        await mgr.DownloadUpdatesAsync(newVersion);
        
        var box = MessageBoxManager
            .GetMessageBoxStandard("Update downloaded", "The update is downloaded, the application will restart to apply it.");

        await box.ShowAsync();
        
        mgr.ApplyUpdatesAndRestart(newVersion);
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        await UpdateMyApp();
    }
}