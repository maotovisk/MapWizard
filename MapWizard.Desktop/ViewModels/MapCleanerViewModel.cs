using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using BeatmapParser;
using BeatmapParser.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.MapCleanerService;
using MapWizard.Desktop.Services.MemoryService;
using MapWizard.Desktop.Utils;
using MapWizard.Tools.MapCleaner;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MapWizard.Desktop.ViewModels;

public partial class MapCleanerViewModel(
    IFilesService filesService,
    IMapCleanerService mapCleanerService,
    IOsuMemoryReaderService osuMemoryReaderService,
    ISettingsService settingsService,
    ISongLibraryService songLibraryService,
    ISukiDialogManager dialogManager,
    ISukiToastManager toastManager) : ViewModelBase
{
    [ObservableProperty] private SelectedMap _originBeatmap = new();

    [ObservableProperty] private bool _analyzeSamples = true;
    [ObservableProperty] private bool _resnapObjects = true;
    [ObservableProperty] private bool _resnapSliderEnds = true;
    [ObservableProperty] private bool _resnapGreenLines = true;
    [ObservableProperty] private bool _resnapBookmarks;
    [ObservableProperty] private bool _removeHitSounds;
    [ObservableProperty] private bool _removeUnusedSamples;
    [ObservableProperty] private bool _removeMuting;
    [ObservableProperty] private bool _muteUnclickableHitsounds;
    [ObservableProperty] private bool _removeUnusedInheritedTimingPoints = true;

    [ObservableProperty] private string _customSnapInput = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _activeSnapDivisors = ["1/8", "1/12"];

    [NotifyPropertyChangedFor(nameof(HasHeaderBackgroundImage))]
    [ObservableProperty] private Bitmap? _headerBackgroundImage;

    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originArtist = string.Empty;

    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originSongName = string.Empty;

    public IReadOnlyList<string> SnapPresets { get; } =
    [
        "1/2", "1/3", "1/4", "1/5", "1/6", "1/7", "1/8", "1/9",
        "1/10", "1/11", "1/12", "1/13", "1/14", "1/15", "1/16"
    ];

    public bool HasHeaderBackgroundImage => HeaderBackgroundImage is not null;

    public string OriginContextTopLine
    {
        get
        {
            var artist = string.IsNullOrWhiteSpace(OriginArtist) ? "Unknown Artist" : OriginArtist;
            var song = string.IsNullOrWhiteSpace(OriginSongName) ? "Unknown Title" : OriginSongName;
            return $"{artist} - {song}";
        }
    }

    // Kept for BeatmapSelectionPanel binding compatibility (destination section is hidden in this view).
    [ObservableProperty] private bool _hasMultiple;

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))]
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [new SelectedMap()];

    public ObservableCollection<SelectedMap> AdditionalBeatmaps
    {
        get => new ObservableCollection<SelectedMap>(DestinationBeatmaps.Skip(1));
        set => DestinationBeatmaps = new ObservableCollection<SelectedMap>(new[] { DestinationBeatmaps.First() }.Concat(value));
    }

    [RelayCommand]
    private void RemoveMap(string _)
    {
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
            LoadOriginBeatmapHeader();
        }
        catch (Exception ex)
        {
            toastManager.ShowToast(NotificationType.Error, "Map Cleaner", ex.Message);
        }
    }

    [RelayCommand]
    private Task PickDestinationFile(CancellationToken _)
    {
        return Task.CompletedTask;
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
        LoadOriginBeatmapHeader();
    }

    [RelayCommand]
    private void AddDestinationFromMemory()
    {
    }

    [RelayCommand]
    private void ReanalyzeOriginAfterPathChange()
    {
        LoadOriginBeatmapHeader();
    }

    [RelayCommand]
    private void AddSnapFromFlyout(string snap)
    {
        if (!TryNormalizeSnap(snap, out var normalized))
        {
            return;
        }

        AddSnapDivisor(normalized);
    }

    [RelayCommand]
    private void AddCustomSnap()
    {
        if (!TryNormalizeSnap(CustomSnapInput, out var normalized))
        {
            toastManager.ShowToast(NotificationType.Error, "Map Cleaner", "Invalid snap format. Use values like 1/8 or 2/3.");
            return;
        }

        AddSnapDivisor(normalized);
        CustomSnapInput = string.Empty;
    }

    [RelayCommand]
    private void RemoveSnap(string snap)
    {
        if (ActiveSnapDivisors.Count <= 1)
        {
            toastManager.ShowToast(NotificationType.Warning, "Map Cleaner", "At least one snap divisor is required.");
            return;
        }

        ActiveSnapDivisors.Remove(snap);
    }

    [RelayCommand]
    private void CleanMaps()
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            toastManager.ShowToast(NotificationType.Error, "Map Cleaner", "Please select a beatmap first.");
            return;
        }

        var options = new MapCleanerOptions
        {
            AnalyzeSamples = AnalyzeSamples,
            ResnapObjects = ResnapObjects,
            ResnapSliderEnds = ResnapSliderEnds,
            ResnapGreenLines = ResnapGreenLines,
            ResnapBookmarks = ResnapBookmarks,
            RemoveUnusedInheritedTimingPoints = RemoveUnusedInheritedTimingPoints,
            RemoveHitSounds = RemoveHitSounds,
            RemoveUnusedSamples = RemoveUnusedSamples,
            RemoveMuting = RemoveMuting,
            MuteUnclickableHitsounds = MuteUnclickableHitsounds,
            SnapDivisors = ActiveSnapDivisors.ToList()
        };

        var success = mapCleanerService.CleanMaps([OriginBeatmap.Path], options, out var result);

        if (success)
        {
            toastManager.ShowToast(
                NotificationType.Success,
                "Map Cleaner",
                $"Done. Resnapped {result.ObjectsResnapped} objects, {result.SliderEndsResnapped} slider ends, and {result.GreenLinesResnapped} greenlines; removed {result.InheritedTimingPointsRemoved} greenlines.");

            LoadOriginBeatmapHeader();
            return;
        }

        var message = result.FailureDetails.Count > 0 ? result.FailureDetails[0] : "Map cleaning failed.";
        toastManager.ShowToast(NotificationType.Error, "Map Cleaner", message);
    }

    private void SetOriginBeatmapPath(string beatmapPath)
    {
        OriginBeatmap = new SelectedMap
        {
            Path = beatmapPath
        };
    }

    private void LoadOriginBeatmapHeader()
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path) || !File.Exists(OriginBeatmap.Path))
        {
            ClearOriginBeatmapHeader();
            return;
        }

        try
        {
            var beatmap = Beatmap.Decode(File.ReadAllText(OriginBeatmap.Path));
            OriginArtist = string.IsNullOrWhiteSpace(beatmap.MetadataSection.ArtistUnicode)
                ? beatmap.MetadataSection.Artist
                : beatmap.MetadataSection.ArtistUnicode;
            OriginSongName = string.IsNullOrWhiteSpace(beatmap.MetadataSection.TitleUnicode)
                ? beatmap.MetadataSection.Title
                : beatmap.MetadataSection.TitleUnicode;

            var backgroundRelativePath = beatmap.Events.EventList
                .OfType<Background>()
                .Select(background => background.Filename)
                .FirstOrDefault(filename => !string.IsNullOrWhiteSpace(filename));

            var resolvedBackgroundPath = MapsetAssetPathUtils.ResolveRelativePathFromBeatmap(OriginBeatmap.Path, backgroundRelativePath);

            HeaderBackgroundImage?.Dispose();
            HeaderBackgroundImage = null;

            if (!string.IsNullOrWhiteSpace(resolvedBackgroundPath) && File.Exists(resolvedBackgroundPath))
            {
                HeaderBackgroundImage = new Bitmap(resolvedBackgroundPath);
            }
        }
        catch
        {
            ClearOriginBeatmapHeader();
        }
    }

    private void ClearOriginBeatmapHeader()
    {
        OriginArtist = string.Empty;
        OriginSongName = string.Empty;

        HeaderBackgroundImage?.Dispose();
        HeaderBackgroundImage = null;
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
            "Map Cleaner",
            allowMultiple,
            token,
            preferredMapsetDirectoryPath);

    private void AddSnapDivisor(string divisor)
    {
        if (ActiveSnapDivisors.Contains(divisor, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        ActiveSnapDivisors.Add(divisor);
        ReorderActiveSnapDivisors();
    }

    private void ReorderActiveSnapDivisors()
    {
        var ordered = ActiveSnapDivisors
            .OrderBy(GetSnapSortKey)
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ActiveSnapDivisors = new ObservableCollection<string>(ordered);
    }

    private static int GetSnapSortKey(string snap)
    {
        if (!TryNormalizeSnap(snap, out var normalized))
        {
            return int.MaxValue;
        }

        var split = normalized.Split('/');
        var numerator = int.Parse(split[0]);
        var denominator = int.Parse(split[1]);

        return (denominator * 1000) + numerator;
    }

    private static bool TryNormalizeSnap(string rawSnap, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(rawSnap))
        {
            return false;
        }

        var parts = rawSnap.Trim()
            .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var numerator) || !int.TryParse(parts[1], out var denominator))
        {
            return false;
        }

        if (numerator <= 0 || denominator <= 0)
        {
            return false;
        }

        normalized = $"{numerator}/{denominator}";
        return true;
    }
}
