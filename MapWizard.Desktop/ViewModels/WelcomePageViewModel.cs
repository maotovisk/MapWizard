using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Services;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Toasts;
using Velopack;

namespace MapWizard.Desktop.ViewModels;

public partial class WelcomePageViewModel(
    ISukiDialogManager dialogManager,
    ISukiToastManager toastManager,
    IUpdateService updateService) : ViewModelBase
{
    public string Message { get; set; } = "Welcome to MapWizard, select a tool to get started!";

    private bool _startupCheckDone;

    public async Task CheckForUpdatesOnStartupAsync()
    {
        if (_startupCheckDone)
        {
            return;
        }

        _startupCheckDone = true;
        await CheckForUpdatesCoreAsync(showNotInstalledMessage: false);
    }

    [RelayCommand]
    private Task CheckForUpdates()
    {
        return CheckForUpdatesCoreAsync(showNotInstalledMessage: true);
    }

    private async Task CheckForUpdatesCoreAsync(bool showNotInstalledMessage)
    {
        if (!updateService.IsInstalled)
        {
            if (showNotInstalledMessage)
            {
                await dialogManager.CreateDialog()
                    .WithTitle("Update Check")
                    .WithContent("MapWizard is not installed. Please install it to check for updates.")
                    .WithOkResult("Ok")
                    .TryShowAsync();
            }

            return;
        }

        var checkingToast = toastManager.CreateToast()
            .OfType(NotificationType.Information)
            .WithLoadingState(true)
            .WithTitle("Updates")
            .WithContent("Checking for updates...")
            .Dismiss().ByClicking()
            .Queue();

        UpdateInfo? newVersion;
        try
        {
            newVersion = await updateService.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            toastManager.Dismiss(checkingToast);
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("Update error")
                .WithContent(ex.Message)
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(6))
                .Queue();
            return;
        }

        toastManager.Dismiss(checkingToast);

        if (newVersion == null)
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Information)
                .WithTitle("Updates")
                .WithContent("You are up to date.")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(4))
                .Queue();
            return;
        }

        toastManager.CreateToast()
            .OfType(NotificationType.Information)
            .WithTitle("Updates")
            .WithContent($"New version {newVersion.TargetFullRelease.Version} is available.")
            .WithActionButton("Later", _ => { }, true, SukiButtonStyles.Flat)
            .WithActionButton("Update", _toast =>
            {
                _ = ShowUpdateToastWithProgressAsync(newVersion);
            }, true, SukiButtonStyles.Accent)
            .Dismiss().ByClicking()
            .Queue();
    }

    private async Task ShowUpdateToastWithProgressAsync(UpdateInfo info)
    {
        var progress = new ProgressBar { Value = 0, ShowProgressText = true };
        var downloadingToast = toastManager.CreateToast()
            .WithTitle("Downloading Update...")
            .WithContent(progress)
            .Dismiss().ByClicking()
            .Queue();

        try
        {
            await updateService.DownloadUpdatesAsync(info, percentage =>
            {
                Dispatcher.UIThread.Post(() => { progress.Value = percentage; });
            });
        }
        catch (Exception ex)
        {
            toastManager.Dismiss(downloadingToast);
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("Update error")
                .WithContent(ex.Message)
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(6))
                .Queue();
            return;
        }

        toastManager.Dismiss(downloadingToast);

        toastManager.CreateToast()
            .OfType(NotificationType.Success)
            .WithTitle("Update Downloaded")
            .WithContent("The update has been downloaded. Please restart the app to apply the update.")
            .WithActionButton("Next Restart", _ => { updateService.WaitExitThenApplyUpdates(info); }, true)
            .WithActionButton("Restart Now", _ => { updateService.ApplyUpdatesAndRestart(info); }, true, SukiButtonStyles.Accent)
            .Dismiss().ByClicking()
            .Queue();
    }
}
