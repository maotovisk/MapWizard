using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.MemoryService;
using MapWizard.Desktop.Views.Controls;
using MapWizard.Desktop.Views.Dialogs;

namespace MapWizard.Desktop.Utils;

public static class BeatmapSelectionUtils
{
    public static async Task<string?> TryGetBeatmapFromOsuAsync(
        IOsuMemoryReaderService osuMemoryReaderService,
        ILazerLookupService lazerLookupService,
        IModalService modalService,
        Action<NotificationType, string, string> showToast,
        string memoryErrorTitle,
        string defaultMemoryErrorMessage,
        string emptyMemoryTitle,
        string emptyMemoryMessage,
        CancellationToken cancellationToken = default)
    {
        var lazerResult = lazerLookupService.GetMountedBeatmapPaths();
        var stableResult = osuMemoryReaderService.GetBeatmapPath();

        if (lazerResult.Status == ResultStatus.Success && lazerResult.Value is { Count: > 0 } mountedBeatmaps)
        {
            var stableBeatmapPath = stableResult.Status == ResultStatus.Success &&
                                    !string.IsNullOrWhiteSpace(stableResult.Value)
                ? stableResult.Value
                : null;

            return await ShowBeatmapSourcePickerAsync(
                modalService,
                mountedBeatmaps,
                stableBeatmapPath,
                showToast,
                cancellationToken);
        }

        return ResolveBeatmapFromMemoryResult(
            stableResult,
            showToast,
            memoryErrorTitle,
            lazerResult.Status == ResultStatus.Error
                ? lazerResult.ErrorMessage ?? defaultMemoryErrorMessage
                : defaultMemoryErrorMessage,
            emptyMemoryTitle,
            emptyMemoryMessage);
    }

    public static string? TryGetBeatmapFromMemory(
        IOsuMemoryReaderService osuMemoryReaderService,
        Action<NotificationType, string, string> showToast,
        string memoryErrorTitle,
        string defaultMemoryErrorMessage,
        string emptyMemoryTitle,
        string emptyMemoryMessage)
    {
        return ResolveBeatmapFromMemoryResult(
            osuMemoryReaderService.GetBeatmapPath(),
            showToast,
            memoryErrorTitle,
            defaultMemoryErrorMessage,
            emptyMemoryTitle,
            emptyMemoryMessage);
    }

    private static string? ResolveBeatmapFromMemoryResult(
        Result<string> currentBeatmap,
        Action<NotificationType, string, string> showToast,
        string memoryErrorTitle,
        string defaultMemoryErrorMessage,
        string emptyMemoryTitle,
        string emptyMemoryMessage)
    {
        if (currentBeatmap.Status == ResultStatus.Error)
        {
            showToast(
                NotificationType.Error,
                memoryErrorTitle,
                currentBeatmap.ErrorMessage ?? defaultMemoryErrorMessage);
            return null;
        }

        if (string.IsNullOrWhiteSpace(currentBeatmap.Value))
        {
            showToast(NotificationType.Error, emptyMemoryTitle, emptyMemoryMessage);
            return null;
        }

        return currentBeatmap.Value;
    }

    private static async Task<string?> ShowBeatmapSourcePickerAsync(
        IModalService modalService,
        IReadOnlyList<string> mountedBeatmaps,
        string? stableBeatmapPath,
        Action<NotificationType, string, string> showToast,
        CancellationToken cancellationToken)
    {
        var content = new StackPanel
        {
            Width = 580,
            Spacing = 12
        };

        content.Children.Add(new ImportantNotice
        {
            Message = "Keep lazer's external-edit screen open until MapWizard has finished."
        });

        var sourceCards = new StackPanel { Spacing = 12 };
        var cards = new List<BeatmapSourceCard>();

        void AddSourceCard(
            BeatmapSourceKind sourceKind,
            IReadOnlyList<string> paths,
            Panel? parent = null)
        {
            var card = new BeatmapSourceCard(sourceKind, paths);
            card.DifficultySelected += OnDifficultySelected;
            cards.Add(card);
            (parent ?? sourceCards).Children.Add(card);
        }

        void OnDifficultySelected(string path) => _ = modalService.CloseAsync(path);

        AddSourceCard(BeatmapSourceKind.Lazer, mountedBeatmaps);
        if (!string.IsNullOrWhiteSpace(stableBeatmapPath))
        {
            var fallbackSection = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Currently on osu!stable",
                        FontSize = 11,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        Opacity = 0.66,
                        Margin = new Avalonia.Thickness(2, 0, 0, 0)
                    }
                }
            };

            AddSourceCard(BeatmapSourceKind.Stable, [stableBeatmapPath], fallbackSection);
            sourceCards.Children.Add(fallbackSection);
        }

        content.Children.Add(new ScrollViewer
        {
            MaxHeight = 520,
            Content = sourceCards,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        });

        try
        {
            return await modalService.ShowAsync(
                new ModalRequest(content, "Select lazer difficulty"),
                cancellationToken) as string;
        }
        catch (InvalidOperationException)
        {
            showToast(
                NotificationType.Warning,
                "osu!lazer",
                "Could not open the difficulty picker because another dialog is already open.");
            return null;
        }
        finally
        {
            foreach (var card in cards)
            {
                card.DifficultySelected -= OnDifficultySelected;
                card.Dispose();
            }
        }
    }

    public static ObservableCollection<SelectedMap> NormalizeDestinationBeatmaps(IReadOnlyCollection<string> beatmapPaths)
    {
        var normalizedPaths = beatmapPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new SelectedMap { Path = path })
            .ToList();

        return new ObservableCollection<SelectedMap>(normalizedPaths);
    }

    public static bool TryAppendDestinationBeatmap(
        ObservableCollection<SelectedMap> destinationBeatmaps,
        string beatmapPath,
        out ObservableCollection<SelectedMap> updatedDestinationBeatmaps)
    {
        var workingSet = destinationBeatmaps;
        if (workingSet.Count == 0 ||
            (workingSet.Count == 1 && string.IsNullOrWhiteSpace(workingSet[0].Path)))
        {
            workingSet = [];
        }

        if (workingSet.Any(x => string.Equals(x.Path, beatmapPath, StringComparison.OrdinalIgnoreCase)))
        {
            updatedDestinationBeatmaps = destinationBeatmaps;
            return false;
        }

        updatedDestinationBeatmaps = new ObservableCollection<SelectedMap>(workingSet.Append(new SelectedMap
        {
            Path = beatmapPath
        }));

        return true;
    }

    public static bool TryAppendDestinationBeatmaps(
        ObservableCollection<SelectedMap> destinationBeatmaps,
        IEnumerable<string> beatmapPaths,
        out ObservableCollection<SelectedMap> updatedDestinationBeatmaps,
        out int addedCount)
    {
        var workingSet = destinationBeatmaps;
        if (workingSet.Count == 0 ||
            (workingSet.Count == 1 && string.IsNullOrWhiteSpace(workingSet[0].Path)))
        {
            workingSet = [];
        }

        var existingPaths = new HashSet<string>(
            workingSet
                .Select(map => map.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path)),
            StringComparer.OrdinalIgnoreCase);

        var merged = workingSet.ToList();
        addedCount = 0;

        foreach (var candidatePath in beatmapPaths
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!existingPaths.Add(candidatePath))
            {
                continue;
            }

            merged.Add(new SelectedMap { Path = candidatePath });
            addedCount++;
        }

        if (addedCount == 0)
        {
            updatedDestinationBeatmaps = destinationBeatmaps;
            return false;
        }

        updatedDestinationBeatmaps = new ObservableCollection<SelectedMap>(merged);
        return true;
    }

    public static string[] GetSiblingDifficultyPaths(string referenceBeatmapPath)
    {
        if (string.IsNullOrWhiteSpace(referenceBeatmapPath))
        {
            return [];
        }

        try
        {
            var fullPath = Path.GetFullPath(referenceBeatmapPath);
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return [];
            }

            return Directory.EnumerateFiles(directoryPath, "*.osu", SearchOption.TopDirectoryOnly)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return [];
        }
    }

    public static bool TryOpenBeatmapFolder(string beatmapPath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            errorMessage = "Select an origin beatmap first.";
            return false;
        }

        string fullBeatmapPath;
        try
        {
            fullBeatmapPath = Path.GetFullPath(beatmapPath);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            errorMessage = "The origin beatmap path is invalid.";
            return false;
        }

        var folderPath = Path.GetDirectoryName(fullBeatmapPath);
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            errorMessage = "The origin beatmap folder was not found.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            errorMessage = ex.Message;
            return false;
        }
    }

    public static string GetPreferredDirectoryOrFallback(
        ObservableCollection<SelectedMap> destinationBeatmaps,
        string fallbackPreferredDirectory)
    {
        return destinationBeatmaps.Count == 0
            ? fallbackPreferredDirectory
            : Path.GetDirectoryName(destinationBeatmaps[0].Path) ?? fallbackPreferredDirectory;
    }
}
