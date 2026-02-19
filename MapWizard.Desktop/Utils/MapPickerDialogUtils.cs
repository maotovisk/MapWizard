using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views.Dialogs;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MapWizard.Desktop.Utils;

public static class MapPickerDialogUtils
{
    public static async Task<IReadOnlyList<string>?> ShowSongSelectDialogAsync(
        ISukiDialogManager dialogManager,
        ISukiToastManager toastManager,
        ISongLibraryService songLibraryService,
        IFilesService filesService,
        ISettingsService settingsService,
        string featureName,
        bool allowMultiple,
        CancellationToken token,
        string? preferredMapsetDirectoryPath = null)
    {
        var songsPath = SongsPathResolver.ResolveSongsPath(settingsService, songLibraryService);
        var songSelectViewModel = new SongSelectDialogViewModel(
            songLibraryService,
            filesService,
            songsPath,
            allowMultiple,
            preferredMapsetDirectoryPath);

        var dialogContent = new SongSelectDialog
        {
            DataContext = songSelectViewModel
        };

        var completion = new TaskCompletionSource<IReadOnlyList<string>?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var dialogLifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(token);

        void OnSelectionSubmitted(IReadOnlyList<string> selectedPaths)
        {
            completion.TrySetResult(selectedPaths);
            dialogManager.DismissDialog();
        }

        songSelectViewModel.SelectionSubmitted += OnSelectionSubmitted;

        try
        {
            var dialogBuilder = dialogManager.CreateDialog()
                .WithTitle("Map Picker")
                .WithContent(dialogContent)
                .WithActionButton("Close", _ => { }, true, "Flat")
                .Dismiss().ByClickingBackground()
                .OnDismissed(_ =>
                {
                    try
                    {
                        dialogLifetimeCts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // The dialog completion path can dispose this CTS before the dismissed callback runs.
                    }

                    completion.TrySetResult(null);
                });

            if (allowMultiple)
            {
                dialogBuilder = dialogBuilder.WithActionButton(
                    "Use Selected",
                    _ => songSelectViewModel.ConfirmSelectionCommand.Execute(null),
                    false,
                    "Success");
            }

            var shown = dialogBuilder.TryShow();
            if (!shown)
            {
                toastManager.ShowToast(
                    NotificationType.Warning,
                    featureName,
                    "Could not open Map Picker because another dialog is already open.");
                return null;
            }

            _ = songSelectViewModel.InitializeAsync(dialogLifetimeCts.Token);
            return await completion.Task;
        }
        finally
        {
            songSelectViewModel.SelectionSubmitted -= OnSelectionSubmitted;
            dialogContent.DataContext = null;
            songSelectViewModel.Dispose();
            dialogLifetimeCts.Dispose();
        }
    }
}
