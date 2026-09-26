using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Layout;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.MemoryService;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views.Dialogs;

namespace MapWizard.Desktop.Utils;

public static class MapPickerDialogUtils
{
    public static async Task<IReadOnlyList<string>?> ShowSongSelectDialogAsync(
        IModalService modalService,
        INotificationService notificationService,
        ISongLibraryService songLibraryService,
        IFilesService filesService,
        ILazerLookupService lazerLookupService,
        IOsuMemoryReaderService osuMemoryReaderService,
        ISettingsService settingsService,
        string featureName,
        bool allowMultiple,
        CancellationToken token,
        string? preferredMapsetDirectoryPath = null)
    {
        // Auto-detection can enumerate processes and probe several filesystem locations.
        // Resolve it away from Avalonia's dispatcher, then create the dialog controls on
        // the captured UI context after the await.
        var songsPath = await Task.Run(
            () => SongsPathResolver.ResolveSongsPath(settingsService, songLibraryService),
            token);
        token.ThrowIfCancellationRequested();

        var songSelectViewModel = new SongSelectDialogViewModel(
            songLibraryService,
            filesService,
            lazerLookupService,
            osuMemoryReaderService,
            songsPath,
            allowMultiple,
            preferredMapsetDirectoryPath);

        var dialogContent = new SongSelectDialog
        {
            DataContext = songSelectViewModel
        };
        // The close action lives in the picker toolbar (X icon button).
        dialogContent.PickerCloseRequested += (_, _) => _ = modalService.CloseAsync(null);

        // No footer Close button — the X icon frees vertical space for the list.
        var footerPanel = BuildFooterPanel(songSelectViewModel, allowMultiple);
        var title = allowMultiple ? "Select destination beatmap(s)" : "Select beatmap";

        var dialogLifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(token);

        async void OnSelectionSubmitted(IReadOnlyList<string> selectedPaths)
        {
            await modalService.CloseAsync(selectedPaths);
        }

        songSelectViewModel.SelectionSubmitted += OnSelectionSubmitted;

        try
        {
            _ = songSelectViewModel.InitializeAsync(dialogLifetimeCts.Token);
            var result = await modalService.ShowAsync(
                new ModalRequest(
                    dialogContent,
                    title,
                    footerPanel,
                    Presentation: ModalPresentation.MapPickerOverlay),
                token);
            return result as IReadOnlyList<string>;
        }
        catch (InvalidOperationException)
        {
            notificationService.ShowToast(
                NotificationType.Warning,
                featureName,
                "Could not open Map Picker because another dialog is already open.");
            return null;
        }
        finally
        {
            try
            {
                dialogLifetimeCts.Cancel();
            }
            catch (ObjectDisposedException ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            }

            songSelectViewModel.SelectionSubmitted -= OnSelectionSubmitted;
            dialogContent.DataContext = null;
            songSelectViewModel.Dispose();
            dialogLifetimeCts.Dispose();
        }
    }

    private static StackPanel BuildFooterPanel(
        SongSelectDialogViewModel songSelectViewModel,
        bool allowMultiple)
    {
        var footerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        if (allowMultiple)
        {
            var useSelectedButton = new Button { Content = "Use Selected" };
            useSelectedButton.Classes.Add("Flat");
            useSelectedButton.Classes.Add("Compact");
            useSelectedButton.Bind(
                Button.IsEnabledProperty,
                CompiledBinding.Create(
                    (SongSelectDialogViewModel viewModel) => viewModel.CanConfirmSelection,
                    songSelectViewModel));
            useSelectedButton.Click += (_, _) => songSelectViewModel.ConfirmSelectionCommand.Execute(null);
            footerPanel.Children.Add(useSelectedButton);
        }

        return footerPanel;
    }
}
