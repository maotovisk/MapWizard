using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls.Notifications;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.MemoryService;

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
