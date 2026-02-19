using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls.Notifications;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Utils;

public static class BeatmapSelectionUtils
{
    public static string? TryGetBeatmapFromMemory(
        IOsuMemoryReaderService osuMemoryReaderService,
        Action<NotificationType, string, string> showToast,
        string memoryErrorTitle,
        string defaultMemoryErrorMessage,
        string emptyMemoryTitle,
        string emptyMemoryMessage)
    {
        var currentBeatmap = osuMemoryReaderService.GetBeatmapPath();

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

    public static string GetPreferredDirectoryOrFallback(
        ObservableCollection<SelectedMap> destinationBeatmaps,
        string fallbackPreferredDirectory)
    {
        return destinationBeatmaps.Count == 0
            ? fallbackPreferredDirectory
            : Path.GetDirectoryName(destinationBeatmaps[0].Path) ?? fallbackPreferredDirectory;
    }
}
