using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views.Dialogs;
using MapWizard.Tools.HelperExtensions;
using Velopack;

namespace MapWizard.Desktop.ViewModels;

public partial class WelcomePageViewModel(
    INotificationService notificationService,
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
                await modalService.ShowMessageAsync(
                    "Update Check",
                    "MapWizard is not installed. Please install it to check for updates.");
            }

            return;
        }

        var checkingToast = notificationService.Show(
            NotificationType.Information,
            "Updates",
            "Checking for updates...",
            isBusy: true);

        UpdateInfo? newVersion;
        try
        {
            newVersion = await updateService.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            MapWizardLogger.LogException(ex);
            notificationService.Dismiss(checkingToast);
            notificationService.ShowToast(
                NotificationType.Error,
                "Update error",
                ex.Message,
                TimeSpan.FromSeconds(6));
            return;
        }

        notificationService.Dismiss(checkingToast);

        if (newVersion == null)
        {
            notificationService.ShowToast(
                NotificationType.Information,
                "Updates",
                "You are up to date.",
                TimeSpan.FromSeconds(4));
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
        notificationService.Show(
            NotificationType.Information,
            "Updates",
            $"New version {info.TargetFullRelease.Version} is available.",
            actions:
            [
                new NotificationAction("Later", () => { }),
                new NotificationAction("Update", () => _ = ShowUpdateToastWithProgressAsync(info), IsPrimary: true)
            ]);
    }

    private async Task ShowUpdateToastWithProgressAsync(UpdateInfo info)
    {
        var downloadingToast = notificationService.Show(
            NotificationType.Information,
            "Downloading Update...",
            "Preparing download...",
            progress: 0);

        try
        {
            await updateService.DownloadUpdatesAsync(info, percentage =>
            {
                notificationService.UpdateProgress(downloadingToast, percentage);
            });
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            notificationService.Dismiss(downloadingToast);
            notificationService.ShowToast(
                NotificationType.Error,
                "Update error",
                ex.Message,
                TimeSpan.FromSeconds(6));
            return;
        }

        notificationService.Dismiss(downloadingToast);

        notificationService.Show(
            NotificationType.Success,
            "Update Downloaded",
            "The update has been downloaded. Please restart the app to apply the update.",
            actions:
            [
                new NotificationAction("Next Restart", () => updateService.WaitExitThenApplyUpdates(info)),
                new NotificationAction("Restart Now", () => updateService.ApplyUpdatesAndRestart(info), IsPrimary: true)
            ]);
    }
}
