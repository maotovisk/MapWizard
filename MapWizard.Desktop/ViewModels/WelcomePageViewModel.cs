using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Velopack;

namespace MapWizard.Desktop.ViewModels;

public partial class WelcomePageViewModel(ISukiDialogManager dialogManager, ISukiToastManager toastManager, UpdateManager updateManager) : ViewModelBase
{
    public string Message { get; set; } = "Welcome to MapWizard, select a tool to get started!";
    
    [ObservableProperty]
    private bool _showUpdateToast = false;
    
    private bool checkedOnce = false;
    
    [RelayCommand]
    private void WindowStartup()
    {
        if (checkedOnce || !updateManager.IsInstalled)
        {
            return;
        }
        checkedOnce = true;
        
        CheckForUpdates();
    }
    
    [RelayCommand]
    private void CheckForUpdates()
    {
        if (!updateManager.IsInstalled)
        {
            dialogManager.CreateDialog()
                .WithTitle("Update Check")
                .WithContent("MapWizard is not installed. Please install it to check for updates.")     
                .WithActionButton("Ok ", _ => { }, true) 
                .TryShow();
            return;
        }


        toastManager.CreateToast().OfType(NotificationType.Information)
            .WithLoadingState(true)
            .WithTitle("Updates")
            .WithContent("Checking for updates...")
            .Dismiss().After(TimeSpan.FromSeconds(8))
            .Queue();

        var newVersion = updateManager.CheckForUpdates();
        if (newVersion == null)
        {
            return;
        }
            
        toastManager.CreateToast().OfType(NotificationType.Information)
            .WithLoadingState(false)
            .WithTitle("Updates")
            .WithContent($"New version {newVersion.BaseRelease?.Version} is available.")
            .WithActionButtonNormal("Later", _ => { }, true)
            .WithActionButton("Update", _ => ShowUpdateToastWithProgress(newVersion), true)
            .Queue();
    }
    
    private void ShowUpdateToastWithProgress(UpdateInfo info)
    {
        var progress = new ProgressBar() { Value = 0, ShowProgressText = true };
        var toast = toastManager.CreateToast()
            .WithTitle("Downloading Update...")
            .WithContent(progress)
            .Queue();

        updateManager.DownloadUpdates(info, x =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                progress.Value = x;
                if (progress.Value < 100) return;
                toastManager.Dismiss(toast);
            });
        });
            
        toastManager.CreateToast()
            .OfType(NotificationType.Success)
            .WithTitle("Update Downloaded")
            .WithContent("The update has been downloaded. Please restart the app to apply the update.")
            .WithActionButton("Next Restart", _ => { updateManager.WaitExitThenApplyUpdates(info); }, true)
            .WithActionButtonNormal("Restart Now", _ =>
            {
                updateManager.ApplyUpdatesAndRestart(info);
            }, true)
            .Queue();
    }
}
