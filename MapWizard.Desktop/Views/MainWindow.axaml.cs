using System.Threading.Tasks;
using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Velopack;
using Velopack.Sources;

namespace MapWizard.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateMyApp().Wait();
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
            return; // no update available        var box = MessageBoxManager
                    //     .GetMessageBoxStandard("New update available!", "There is a new update, the application is will restart to apply it.");

                    // await box.ShowAsync();
        
        // download new version
        await mgr.DownloadUpdatesAsync(newVersion);
        
        var box = MessageBoxManager
            .GetMessageBoxStandard("New update available!", "There is a new update, the application will restart to apply it.");

        await box.ShowAsync();
        
        // install new version and restart app
        mgr.ApplyUpdatesAndRestart(newVersion);
    }
}