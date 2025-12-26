using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Toasts;
using Velopack;

namespace MapWizard.Desktop.ViewModels;

public partial class WelcomePageViewModel(ISukiDialogManager dialogManager, ISukiToastManager toastManager, UpdateManager updateManager) : ViewModelBase
{
    public string Message { get; set; } = "Welcome to MapWizard, select a tool to get started!";

    [ObservableProperty]
    private bool _showUpdateToast = false;

    private bool checkedOnce = false;

#if DEBUG
    private const bool SimulateVelopackInDebug = true;
#endif

    [RelayCommand]
    private async Task WindowStartup()
    {
        if (checkedOnce || !updateManager.IsInstalled)
            return;

        checkedOnce = true;
        await CheckForUpdates();
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
#if DEBUG
        // Em Debug, se não estiver instalado, simula toda a experiência.
        if (SimulateVelopackInDebug && !updateManager.IsInstalled)
        {
            await CheckForUpdatesDebug();
            return;
        }
#endif

        if (!updateManager.IsInstalled)
        {
            dialogManager.CreateDialog()
                .WithTitle("Update Check")
                .WithContent("MapWizard is not installed. Please install it to check for updates.")
                .WithActionButton("Ok ", _ => { }, true)
                .TryShow();
            return;
        }

        var checkingToast = toastManager.CreateToast()
            .OfType(NotificationType.Information)
            .WithLoadingState(true)
            .WithTitle("Updates")
            .WithContent("Checking for updates...")
            .Dismiss().ByClicking()
            .Queue();

        UpdateInfo? newVersion = null;
        try
        {
            newVersion = await Task.Run(() => updateManager.CheckForUpdates());
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

        toastManager.CreateToast().OfType(NotificationType.Information)
            .WithLoadingState(false)
            .WithTitle("Updates")
            .WithContent($"New version {newVersion.TargetFullRelease.Version} is available.")
            .WithActionButton("Later", _ => { }, true, SukiButtonStyles.Flat)
            .WithActionButton("Update",
                _ =>
                {
                    Task.Run(() => ShowUpdateToastWithProgress(newVersion));
                }, true, SukiButtonStyles.Accent)
            .Dismiss().ByClicking()
            .Queue();
    }

    private async Task ShowUpdateToastWithProgress(UpdateInfo info)
    {
        var progress = new ProgressBar { Value = 0, ShowProgressText = true };
        var downloadingToast = toastManager.CreateToast()
            .WithTitle("Downloading Update...")
            .WithContent(progress)
            .Dismiss().ByClicking()
            .Queue();

        try
        {
            await Task.Run(() => updateManager.DownloadUpdates(info, x =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    progress.Value = x;
                });
            }));
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
            .WithActionButton("Next Restart", _ => { updateManager.WaitExitThenApplyUpdates(info); }, true)
            .WithActionButton("Restart Now", _ => { updateManager.ApplyUpdatesAndRestart(info); }, true, SukiButtonStyles.Accent)
            .Dismiss().ByClicking()
            .Queue();
    }

#if DEBUG
    private async Task CheckForUpdatesDebug()
    {
        var checkingToast = toastManager.CreateToast()
            .OfType(NotificationType.Information)
            .WithLoadingState(true)
            .WithTitle("Updates")
            .WithContent("Checking for updates (debug)...")
            .Dismiss().ByClicking()
            .Queue();

        // Atraso simulado da request
        await Task.Delay(TimeSpan.FromSeconds(2));

        toastManager.Dismiss(checkingToast);

        toastManager.CreateToast().OfType(NotificationType.Information)
            .WithTitle("Updates")
            .WithContent("New version 9.9.9 (debug) is available.")
            .WithActionButton("Later", _ => { }, true, SukiButtonStyles.Flat)
            .WithActionButton("Update", async void (_) =>
            {
                await ShowUpdateToastWithProgressDebug();
            }, true, SukiButtonStyles.Accent)
            .Dismiss().ByClicking()
            .Queue();
    }

    private async Task ShowUpdateToastWithProgressDebug()
    {
        var progress = new ProgressBar { Value = 0, ShowProgressText = true };
        var downloadingToast = toastManager.CreateToast()
            .WithTitle("Downloading Update (debug)...")
            .WithContent(progress)
            .Dismiss().ByClicking()
            .Queue();

        // Simula download com progresso incremental e jitter
        var rnd = new Random();
        double val = 0;
        while (val < 100)
        {
            await Task.Delay(180 + rnd.Next(0, 220));
            val = Math.Min(100, val + rnd.Next(7, 16));
            var v = val;
            Dispatcher.UIThread.Post(() => progress.Value = v);
        }

        toastManager.Dismiss(downloadingToast);

        toastManager.CreateToast()
            .OfType(NotificationType.Success)
            .WithTitle("Update Downloaded (debug)")
            .WithContent("Simulated update is ready.")
            .WithActionButton("Close", _ => { }, true, SukiButtonStyles.Flat)
            .Dismiss().ByClicking()
            .Queue();
    }
#endif
}
