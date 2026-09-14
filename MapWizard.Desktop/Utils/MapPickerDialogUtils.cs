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
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views.Dialogs;
using SukiUI.Toasts;

namespace MapWizard.Desktop.Utils;

public static class MapPickerDialogUtils
{
    public static async Task<IReadOnlyList<string>?> ShowSongSelectDialogAsync(
        IModalService modalService,
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

        var footerPanel = BuildFooterPanel(modalService, songSelectViewModel, allowMultiple);
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
                new ModalRequest(dialogContent, title, footerPanel),
                token);
            return result as IReadOnlyList<string>;
        }
        catch (InvalidOperationException)
        {
            toastManager.ShowToast(
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
        IModalService modalService,
        SongSelectDialogViewModel songSelectViewModel,
        bool allowMultiple)
    {
        var footerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var closeButton = new Button { Content = "Close" };
        closeButton.Classes.Add("Basic");
        closeButton.Classes.Add("Compact");
        closeButton.Click += (_, _) => _ = modalService.CloseAsync(null);
        footerPanel.Children.Add(closeButton);

        if (allowMultiple)
        {
            var useSelectedButton = new Button { Content = "Use Selected" };
            useSelectedButton.Classes.Add("Flat");
            useSelectedButton.Classes.Add("Compact");
            useSelectedButton.Bind(
                Button.IsEnabledProperty,
                new Binding(nameof(SongSelectDialogViewModel.CanConfirmSelection))
                {
                    Source = songSelectViewModel
                });
            useSelectedButton.Click += (_, _) => songSelectViewModel.ConfirmSelectionCommand.Execute(null);
            footerPanel.Children.Add(useSelectedButton);
        }

        return footerPanel;
    }
}
