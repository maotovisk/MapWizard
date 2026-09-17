using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views.Dialogs;
using MapWizard.Tools.HelperExtensions;
using Velopack;

namespace MapWizard.Desktop.ViewModels;

public partial class WelcomePageViewModel(
    INotificationService notificationService,
    IModalService modalService,
    IUpdateService updateService,
    ISettingsService settingsService,
    ISongLibraryService songLibraryService) : ViewModelBase
{
    public string Message { get; set; } = "Welcome to MapWizard, select a tool to get started!";

    /// <summary>Tools shown in the Start list.</summary>
    public IReadOnlyList<QuickStartTool> Tools { get; } =
    [
        new("copier", "Hitsound Copier", "Copy hitsounds between difficulties.", LucideIconKind.Copy),
        new("metadata", "Metadata Manager", "Edit and sync metadata across a mapset.", LucideIconKind.Files),
        new("hitsound-editor", "HitSound Editor", "Inspect and tweak hitsounds on a timeline.", LucideIconKind.ChartLine),
        new("combo-colour", "Combo Colour Studio", "Design and apply combo colour patterns.", LucideIconKind.Palette),
        new("map-cleaner", "Map Cleaner", "Resnap objects and remove unused timing points.", LucideIconKind.BrushCleaning),
    ];

    [ObservableProperty]
    private bool _isSongsFolderConfigured;

    [ObservableProperty]
    private string _songsFolderStatusText = "osu! Songs folder not set.";

    public void RefreshSongsFolderStatus()
    {
        var songsPath = settingsService.GetMainSettings().SongsPath;
        IsSongsFolderConfigured = songLibraryService.IsValidSongsPath(songsPath);
        SongsFolderStatusText = IsSongsFolderConfigured
            ? $"Songs folder: {songsPath}"
            : "osu! Songs folder not set.";
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        await CheckForUpdatesCoreAsync(showNotInstalledMessage: true);
    }

    [RelayCommand]
    private void OpenDiscord()
    {
        OpenExternalLink(AppLinks.Discord, "Discord");
    }

    [RelayCommand]
    private void OpenDocumentation()
    {
        OpenExternalLink(AppLinks.Documentation, "Documentation");
    }

    [RelayCommand]
    private void OpenGithub()
    {
        OpenExternalLink(AppLinks.Repository, "GitHub");
    }

    private void OpenExternalLink(string url, string title)
    {
        if (!AppLinks.TryOpen(url))
        {
            notificationService.ShowToast(NotificationType.Error, title, "The link could not be opened.");
        }
    }

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

        var updateButton = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new LucideIcon
                    {
                        Kind = LucideIconKind.Download,
                        Size = 16,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "Update",
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
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
