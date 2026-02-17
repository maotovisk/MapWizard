using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views.Dialogs;
using MapWizard.Tools.HitSounds.Copier;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MapWizard.Desktop.ViewModels;

public partial class HitSoundCopierViewModel(
    IFilesService filesService,
    IHitSoundService hitSoundService,
    IOsuMemoryReaderService osuMemoryReaderService,
    ISettingsService settingsService,
    ISongLibraryService songLibraryService,
    ISukiDialogManager dialogManager,
    ISukiToastManager toastManager) : ViewModelBase
{
    [ObservableProperty] private SelectedMap _originBeatmap = new();
    [ObservableProperty] private bool _hasMultiple;
    [ObservableProperty] private bool _copySampleAndVolumeChanges = true;
    [ObservableProperty] private bool _overwriteMuting;
    [ObservableProperty] private bool _overwriteEverything = true;
    [ObservableProperty] private bool _copySliderBodySounds = true;
    [ObservableProperty] private int _leniency = 5;

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))]
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [new SelectedMap()];

    public ObservableCollection<SelectedMap> AdditionalBeatmaps
    {
        get => new ObservableCollection<SelectedMap>(DestinationBeatmaps.Skip(1));
        set
        {
            DestinationBeatmaps =
                new ObservableCollection<SelectedMap>(new[] { DestinationBeatmaps.First() }.Concat(value));
        }
    }

    [ObservableProperty] private string _preferredDirectory = string.Empty;

    [RelayCommand]
    private void RemoveMap(string path)
    {
        DestinationBeatmaps = new ObservableCollection<SelectedMap>(DestinationBeatmaps.Where(x => x.Path != path));
        if (DestinationBeatmaps.Count < 2)
        {
            HasMultiple = false;
        }
    }

    [RelayCommand]
    private async Task PickOriginFile(CancellationToken token)
    {
        try
        {
            var selectedPaths = await ShowSongSelectDialogAsync(allowMultiple: false, token: token);
            if (token.IsCancellationRequested || selectedPaths is null || selectedPaths.Count == 0)
            {
                return;
            }

            SetOriginBeatmapPath(selectedPaths[0]);
        }
        catch (Exception ex)
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("HitSound Copier")
                .WithContent(ex.Message)
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
        }
    }

    [RelayCommand]
    private async Task PickDestinationFile(CancellationToken token)
    {
        try
        {
            var selectedPaths = await ShowSongSelectDialogAsync(
                allowMultiple: true,
                token: token,
                preferredMapsetDirectoryPath: GetOriginMapsetDirectoryPath());
            if (token.IsCancellationRequested || selectedPaths is null || selectedPaths.Count == 0)
            {
                return;
            }

            SetDestinationBeatmaps(selectedPaths);
        }
        catch (Exception ex)
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("HitSound Copier")
                .WithContent(ex.Message)
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
        }
    }

    [RelayCommand]
    private void SetOriginFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();
        if (currentBeatmap is null)
        {
            return;
        }

        SetOriginBeatmapPath(currentBeatmap);
    }

    [RelayCommand]
    private void AddDestinationFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();
        if (currentBeatmap is null)
        {
            return;
        }

        var destinationBeatmaps = DestinationBeatmaps;
        if (destinationBeatmaps.Count == 0 ||
            (destinationBeatmaps.Count == 1 && string.IsNullOrEmpty(destinationBeatmaps.First().Path)))
        {
            destinationBeatmaps = [];
        }

        if (destinationBeatmaps.Any(x =>
                string.Equals(x.Path, currentBeatmap, StringComparison.OrdinalIgnoreCase)))
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("Duplicate Beatmap")
                .WithContent("This beatmap is already in the list.")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
            return;
        }

        destinationBeatmaps = new ObservableCollection<SelectedMap>(destinationBeatmaps.Append(new SelectedMap
        {
            Path = currentBeatmap
        }));

        DestinationBeatmaps = destinationBeatmaps;
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    private void SetOriginBeatmapPath(string beatmapPath)
    {
        OriginBeatmap = new SelectedMap { Path = beatmapPath };
        PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? string.Empty;
    }

    private void SetDestinationBeatmaps(IReadOnlyCollection<string> beatmapPaths)
    {
        var normalizedPaths = beatmapPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new SelectedMap { Path = path })
            .ToList();

        if (normalizedPaths.Count == 0)
        {
            return;
        }

        DestinationBeatmaps = new ObservableCollection<SelectedMap>(normalizedPaths);
        HasMultiple = DestinationBeatmaps.Count > 1;
        PreferredDirectory = Path.GetDirectoryName(DestinationBeatmaps[0].Path) ?? PreferredDirectory;
    }

    private string? GetBeatmapFromMemory()
    {
        var currentBeatmap = osuMemoryReaderService.GetBeatmapPath();

        if (currentBeatmap.Status == ResultStatus.Error)
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("Memory Error")
                .WithContent(currentBeatmap.ErrorMessage ??
                             "Something went wrong while getting the beatmap path from memory.")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
            return null;
        }

        if (string.IsNullOrEmpty(currentBeatmap.Value))
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("No Beatmap")
                .WithContent("No beatmap found in memory.")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
            return null;
        }

        return currentBeatmap.Value;
    }

    private async Task<IReadOnlyList<string>?> ShowSongSelectDialogAsync(
        bool allowMultiple,
        CancellationToken token,
        string? preferredMapsetDirectoryPath = null)
    {
        var songsPath = ResolveSongsPath();
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
                toastManager.CreateToast()
                    .OfType(NotificationType.Warning)
                    .WithTitle("HitSound Copier")
                    .WithContent("Could not open Map Picker because another dialog is already open.")
                    .Dismiss().ByClicking()
                    .Dismiss().After(TimeSpan.FromSeconds(8))
                    .Queue();
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

    private string ResolveSongsPath()
    {
        var settings = settingsService.GetMainSettings();
        var configuredPath = settings.SongsPath;
        if (songLibraryService.IsValidSongsPath(configuredPath))
        {
            return configuredPath;
        }

        var detectedPath = songLibraryService.TryDetectSongsPath();
        if (songLibraryService.IsValidSongsPath(detectedPath))
        {
            settings.SongsPath = detectedPath!;
            settingsService.SaveMainSettings(settings);
            return detectedPath!;
        }

        return string.Empty;
    }

    private string? GetOriginMapsetDirectoryPath()
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            return null;
        }

        try
        {
            return Path.GetDirectoryName(Path.GetFullPath(OriginBeatmap.Path));
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    private void CopyHitSounds()
    {
        var type = NotificationType.Error;
        var message = string.Empty;

        var options = new HitSoundCopierOptions
        {
            CopySampleAndVolumeChanges = CopySampleAndVolumeChanges,
            CopySliderBodySounds = CopySliderBodySounds,
            Leniency = Leniency,
            OverwriteMuting = OverwriteMuting,
            OverwriteEverything = OverwriteEverything
        };

        var destinationPaths = DestinationBeatmaps
            .Select(x => x.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            message = "Please select an origin beatmap!";
        }
        else if (destinationPaths.Length == 0)
        {
            message = "Please select at least one destination beatmap!";
        }
        else if (hitSoundService.CopyHitsoundsAsync(OriginBeatmap.Path, destinationPaths, options))
        {
            type = NotificationType.Success;
            message = $"HitSounds applied successfully to {destinationPaths.Length} beatmap(s)!";
        }

        toastManager.CreateToast()
            .OfType(type)
            .WithTitle("HitSound Copier")
            .WithContent(message)
            .Dismiss().ByClicking()
            .Dismiss().After(TimeSpan.FromSeconds(8))
            .Queue();
    }
}
