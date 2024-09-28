using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MapWizard.Desktop.ViewModels;
using MsBox.Avalonia;
using Velopack;
using Velopack.Sources;

namespace MapWizard.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
    
    
    private static async Task UpdateMyApp()
    {
        var mgr = new UpdateManager(new GithubSource("https://github.com/maotovisk/MapWizard", null, false, null));

        if (!mgr.IsInstalled)
            return; // app is not installed
        
        // check for new version
        var newVersion = await mgr.CheckForUpdatesAsync();
        if (newVersion == null)
            return;
        
        // download new version
        await mgr.DownloadUpdatesAsync(newVersion);
        
        var box = MessageBoxManager
            .GetMessageBoxStandard("New update available!", "There is a new update, the application will restart to apply it.");

        await box.ShowAsync();
        
        // install new version and restart app
        mgr.ApplyUpdatesAndRestart(newVersion);
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        await UpdateMyApp();
    }
}