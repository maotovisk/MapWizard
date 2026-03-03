using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.HitsoundService;
using MapWizard.Desktop.Services.MemoryService;
using MapWizard.Desktop.Utils;
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
    [ObservableProperty] private bool _copyUsedSamplesIfDifferentMapset;
    [ObservableProperty] private int _leniency = 5;

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))]
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [];

    public ObservableCollection<SelectedMap> AdditionalBeatmaps
    {
        get => BeatmapPanelViewModelUtils.GetAdditionalBeatmaps(DestinationBeatmaps);
        set => DestinationBeatmaps = BeatmapPanelViewModelUtils.MergeWithAdditionalBeatmaps(DestinationBeatmaps, value);
    }

    [ObservableProperty] private string _preferredDirectory = string.Empty;

    [RelayCommand]
    private void RemoveMap(string path)
    {
        DestinationBeatmaps = BeatmapPanelViewModelUtils.RemoveDestinationBeatmap(DestinationBeatmaps, path);
        HasMultiple = BeatmapPanelViewModelUtils.HasMultipleDestinationBeatmaps(DestinationBeatmaps);
    }

    [RelayCommand]
    private void ToggleDestinationMap(string path)
    {
        if (!BeatmapPanelViewModelUtils.TryToggleDestinationBeatmap(DestinationBeatmaps, path, out var destinationBeatmaps))
        {
            return;
        }

        DestinationBeatmaps = destinationBeatmaps;
        HasMultiple = BeatmapPanelViewModelUtils.HasMultipleDestinationBeatmaps(DestinationBeatmaps);
    }

    [RelayCommand]
    private async Task PickOriginFile(CancellationToken token)
    {
        try
        {
            var selectedPaths = await ShowSongSelectDialogAsync(
                allowMultiple: false,
                token: token,
                preferredMapsetDirectoryPath: BeatmapPathUtils.TryGetMapsetDirectoryPath(OriginBeatmap.Path));
            if (token.IsCancellationRequested || selectedPaths is null || selectedPaths.Count == 0)
            {
                return;
            }

            SetOriginBeatmapPath(selectedPaths[0]);
        }
        catch (Exception ex)
        {
            toastManager.ShowToast(NotificationType.Error, "HitSound Copier", ex.Message);
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
                preferredMapsetDirectoryPath: BeatmapPathUtils.TryGetMapsetDirectoryPath(OriginBeatmap.Path));
            if (token.IsCancellationRequested || selectedPaths is null || selectedPaths.Count == 0)
            {
                return;
            }

            SetDestinationBeatmaps(selectedPaths);
        }
        catch (Exception ex)
        {
            toastManager.ShowToast(NotificationType.Error, "HitSound Copier", ex.Message);
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

        if (!BeatmapSelectionUtils.TryAppendDestinationBeatmap(DestinationBeatmaps, currentBeatmap, out var destinationBeatmaps))
        {
            toastManager.ShowToast(NotificationType.Error, "Duplicate Beatmap", "This beatmap is already in the list.");
            return;
        }

        DestinationBeatmaps = destinationBeatmaps;
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    [RelayCommand]
    private void OpenOriginFolder()
    {
        if (BeatmapSelectionUtils.TryOpenBeatmapFolder(OriginBeatmap.Path, out var errorMessage))
        {
            return;
        }

        toastManager.ShowToast(
            NotificationType.Warning,
            "HitSound Copier",
            string.IsNullOrWhiteSpace(errorMessage)
                ? "Unable to open the origin beatmap folder."
                : errorMessage);
    }

    [RelayCommand]
    private void AddMapsetDiffsToDestination()
    {
        var referencePath = BeatmapPanelViewModelUtils.ResolveMapsetReferenceBeatmapPath(DestinationBeatmaps, OriginBeatmap.Path);
        if (referencePath is null)
        {
            toastManager.ShowToast(
                NotificationType.Warning,
                "HitSound Copier",
                "Select an origin beatmap (or target beatmaps from one mapset) first.");
            return;
        }

        var siblingDiffs = BeatmapSelectionUtils.GetSiblingDifficultyPaths(referencePath)
            .Where(path => !string.Equals(path, OriginBeatmap.Path, StringComparison.OrdinalIgnoreCase));

        if (!BeatmapSelectionUtils.TryAppendDestinationBeatmaps(
                DestinationBeatmaps,
                siblingDiffs,
                out var updatedDestinationBeatmaps,
                out var addedCount))
        {
            toastManager.ShowToast(
                NotificationType.Warning,
                "HitSound Copier",
                "No additional mapset difficulties were available to add.");
            return;
        }

        DestinationBeatmaps = updatedDestinationBeatmaps;
        HasMultiple = BeatmapPanelViewModelUtils.HasMultipleDestinationBeatmaps(DestinationBeatmaps);
        toastManager.ShowToast(
            NotificationType.Success,
            "HitSound Copier",
            $"Added {addedCount} mapset diff(s) to destination.");
    }

    private void SetOriginBeatmapPath(string beatmapPath)
    {
        OriginBeatmap = new SelectedMap { Path = beatmapPath };
        PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? string.Empty;
    }

    private void SetDestinationBeatmaps(IReadOnlyCollection<string> beatmapPaths)
    {
        if (!BeatmapPanelViewModelUtils.TrySetDestinationBeatmaps(beatmapPaths, out var normalizedBeatmaps))
        {
            return;
        }

        DestinationBeatmaps = normalizedBeatmaps;
        HasMultiple = BeatmapPanelViewModelUtils.HasMultipleDestinationBeatmaps(DestinationBeatmaps);
        PreferredDirectory = BeatmapPanelViewModelUtils.GetPreferredDirectoryOrFallback(DestinationBeatmaps, PreferredDirectory);
    }

    private string? GetBeatmapFromMemory()
    {
        return BeatmapSelectionUtils.TryGetBeatmapFromMemory(
            osuMemoryReaderService,
            (type, title, message) => toastManager.ShowToast(type, title, message),
            "Memory Error",
            "Something went wrong while getting the beatmap path from memory.",
            "No Beatmap",
            "No beatmap found in memory.");
    }

    private Task<IReadOnlyList<string>?> ShowSongSelectDialogAsync(
        bool allowMultiple,
        CancellationToken token,
        string? preferredMapsetDirectoryPath = null)
        => MapPickerDialogUtils.ShowSongSelectDialogAsync(
            dialogManager,
            toastManager,
            songLibraryService,
            filesService,
            settingsService,
            "HitSound Copier",
            allowMultiple,
            token,
            preferredMapsetDirectoryPath);

    [RelayCommand]
    private async Task CopyHitSounds()
    {
        var type = NotificationType.Error;
        string message;

        try
        {
            var options = new HitSoundCopierOptions
            {
                CopySampleAndVolumeChanges = CopySampleAndVolumeChanges,
                CopySliderBodySounds = CopySliderBodySounds,
                CopyUsedSamplesIfDifferentMapset = CopyUsedSamplesIfDifferentMapset,
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
            else
            {
                var compatibility = hitSoundService.AnalyzeTimingCompatibility(OriginBeatmap.Path, destinationPaths);

                if (compatibility.HasTimingMismatch)
                {
                    var shouldProceed = await dialogManager.CreateDialog()
                        .WithTitle("Timing Mismatch Warning")
                        .WithContent(BuildTimingMismatchDialogContent(compatibility))
                        .WithYesNoResult("Copy Anyway", "Cancel")
                        .TryShowAsync();

                    if (!shouldProceed)
                    {
                        return;
                    }
                }
                else if (compatibility.HasOffsetOnlyMismatch)
                {
                    var suggestedLeniency = Math.Max(Leniency, compatibility.SuggestedLeniencyMs);
                    var shouldProceed = await dialogManager.CreateDialog()
                        .WithTitle("Offset Detected")
                        .WithContent(BuildOffsetDialogContent(compatibility, suggestedLeniency))
                        .WithYesNoResult($"Copy with {suggestedLeniency}ms leniency", "Cancel")
                        .TryShowAsync();

                    if (!shouldProceed)
                    {
                        return;
                    }

                    options.Leniency = suggestedLeniency;
                    Leniency = suggestedLeniency;
                }

                if (hitSoundService.CopyHitsoundsAsync(OriginBeatmap.Path, destinationPaths, options))
                {
                    type = NotificationType.Success;
                    message = $"HitSounds applied successfully to {destinationPaths.Length} beatmap(s)!";
                }
                else
                {
                    message = "Failed to copy hitsounds. Check the console/log output for more details.";
                }
            }
        }
        catch (Exception ex)
        {
            message = ex.Message;
        }

        toastManager.ShowToast(type, "HitSound Copier", message);
    }

    private static string BuildOffsetDialogContent(HitSoundTimingCompatibilityReport compatibility, int suggestedLeniency)
    {
        var builder = new StringBuilder();
        builder.AppendLine("A constant timing offset was detected between the origin and target redlines.");
        builder.AppendLine($"Suggested leniency: {suggestedLeniency} ms.");
        builder.AppendLine();
        builder.AppendLine(compatibility.OffsetOnlyMismatchCount == 1
            ? "Affected target:"
            : $"Affected targets ({compatibility.OffsetOnlyMismatchCount}):");
        AppendTargetList(
            builder,
            compatibility.Targets.Where(x => x.Kind == HitSoundTimingCompatibilityKind.OffsetOnlyMismatch).Select(x => x.TargetPath));
        return builder.ToString().TrimEnd();
    }

    private static string BuildTimingMismatchDialogContent(HitSoundTimingCompatibilityReport compatibility)
    {
        var builder = new StringBuilder();
        builder.AppendLine("The target timing differs from the origin (different redlines/BPMs).");
        builder.AppendLine("Hitsounds can still be copied, but alignment may be incorrect.");
        builder.AppendLine();

        if (compatibility.TimingMismatchCount > 0)
        {
            builder.AppendLine(compatibility.TimingMismatchCount == 1
                ? "Timing-mismatched target:"
                : $"Timing-mismatched targets ({compatibility.TimingMismatchCount}):");
            AppendTargetList(
                builder,
                compatibility.Targets.Where(x => x.Kind == HitSoundTimingCompatibilityKind.TimingMismatch).Select(x => x.TargetPath));
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendTargetList(StringBuilder builder, IEnumerable<string> targetPaths)
    {
        const int maxDisplayed = 5;
        var names = targetPaths
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Take(maxDisplayed + 1)
            .ToList();

        for (var i = 0; i < Math.Min(maxDisplayed, names.Count); i++)
        {
            builder.AppendLine($"- {names[i]}");
        }

        if (names.Count > maxDisplayed)
        {
            builder.AppendLine($"...and {names.Count - maxDisplayed} more.");
        }
    }
}
