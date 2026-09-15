using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views.Dialogs;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Toasts;
using Velopack;

namespace MapWizard.Desktop.ViewModels;

public partial class WelcomePageViewModel(
    ISukiDialogManager dialogManager,
    ISukiToastManager toastManager,
    IModalService modalService,
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
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
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

        await ShowUpdateAvailableModalAsync(newVersion);
    }

    private async Task ShowUpdateAvailableModalAsync(UpdateInfo info)
    {
        var content = new UpdateAvailableDialog();
        content.Configure(info, updateService.VersionLabel);

        var footerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var laterButton = new Button { Content = "Later" };
        laterButton.Classes.Add("Basic");
        laterButton.Classes.Add("Compact");
        laterButton.Click += async (_, _) => await modalService.CloseAsync(null);
        footerPanel.Children.Add(laterButton);

        var updateButton = new Button { Content = "Update" };
        updateButton.Classes.Add("Flat");
        updateButton.Classes.Add("Compact");
        updateButton.Click += async (_, _) => await modalService.CloseAsync("Update");
        footerPanel.Children.Add(updateButton);

        object? result;
        try
        {
            result = await modalService.ShowAsync(
                new ModalRequest(content, "Update Available", footerPanel));
        }
        catch (InvalidOperationException)
        {
            ShowUpdateAvailableToast(info);
            return;
        }

        if (result as string == "Update")
        {
            await ShowUpdateToastWithProgressAsync(info);
        }
    }

    private void ShowUpdateAvailableToast(UpdateInfo info)
    {
        toastManager.CreateToast()
            .OfType(NotificationType.Information)
            .WithTitle("Updates")
            .WithContent($"New version {info.TargetFullRelease.Version} is available.")
            .WithActionButton("Later", _ => { }, true, SukiButtonStyles.Flat)
            .WithActionButton("Update", _toast =>
            {
                _ = ShowUpdateToastWithProgressAsync(info);
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
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
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
