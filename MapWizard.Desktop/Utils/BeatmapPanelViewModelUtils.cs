using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Utils;

public static class BeatmapPanelViewModelUtils
{
    public static ObservableCollection<SelectedMap> GetAdditionalBeatmaps(
        IReadOnlyCollection<SelectedMap> destinationBeatmaps)
    {
        return new ObservableCollection<SelectedMap>(destinationBeatmaps.Skip(1));
    }

    public static ObservableCollection<SelectedMap> MergeWithAdditionalBeatmaps(
        IReadOnlyCollection<SelectedMap> destinationBeatmaps,
        IEnumerable<SelectedMap> additionalBeatmaps)
    {
        var first = destinationBeatmaps.FirstOrDefault() ?? new SelectedMap();
        return new ObservableCollection<SelectedMap>(new[] { first }.Concat(additionalBeatmaps));
    }

    public static ObservableCollection<SelectedMap> RemoveDestinationBeatmap(
        IReadOnlyCollection<SelectedMap> destinationBeatmaps,
        string path)
    {
        return new ObservableCollection<SelectedMap>(
            destinationBeatmaps.Where(x => !string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)));
    }

    public static bool TryToggleDestinationBeatmap(
        IReadOnlyCollection<SelectedMap> destinationBeatmaps,
        string path,
        out ObservableCollection<SelectedMap> updatedDestinationBeatmaps)
    {
        updatedDestinationBeatmaps = new ObservableCollection<SelectedMap>(destinationBeatmaps);

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (destinationBeatmaps.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            updatedDestinationBeatmaps = RemoveDestinationBeatmap(destinationBeatmaps, path);
            return true;
        }

        return BeatmapSelectionUtils.TryAppendDestinationBeatmap(
            new ObservableCollection<SelectedMap>(destinationBeatmaps),
            path,
            out updatedDestinationBeatmaps);
    }

    public static bool TrySetDestinationBeatmaps(
        IReadOnlyCollection<string> beatmapPaths,
        out ObservableCollection<SelectedMap> normalizedDestinationBeatmaps)
    {
        normalizedDestinationBeatmaps = BeatmapSelectionUtils.NormalizeDestinationBeatmaps(beatmapPaths);
        return normalizedDestinationBeatmaps.Count > 0;
    }

    public static bool HasMultipleDestinationBeatmaps(IReadOnlyCollection<SelectedMap> destinationBeatmaps)
    {
        return destinationBeatmaps.Count > 1;
    }

    public static string GetPreferredDirectoryOrFallback(
        ObservableCollection<SelectedMap> destinationBeatmaps,
        string fallbackDirectory)
    {
        return BeatmapSelectionUtils.GetPreferredDirectoryOrFallback(destinationBeatmaps, fallbackDirectory);
    }

    public static string? ResolveMapsetReferenceBeatmapPath(
        IReadOnlyCollection<SelectedMap> destinationBeatmaps,
        string originBeatmapPath)
    {
        var destinationPaths = destinationBeatmaps
            .Select(map => map.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        if (destinationPaths.Length > 0)
        {
            var distinctFolders = destinationPaths
                .Select(BeatmapPathUtils.TryGetMapsetDirectoryPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (distinctFolders.Length == 1)
            {
                return destinationPaths[0];
            }
        }

        return !string.IsNullOrWhiteSpace(originBeatmapPath) ? originBeatmapPath : null;
    }
}
