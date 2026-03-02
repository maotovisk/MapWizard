using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using BeatmapParser;
using BeatmapParser.Enums.Storyboard;
using BeatmapParser.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Utils;
using MapWizard.Tools.MetadataManager;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Color = System.Drawing.Color;

namespace MapWizard.Desktop.ViewModels;

public partial class MetadataManagerViewModel(
    IFilesService filesService,
    IMetadataManagerService metadataManagerService,
    IOsuMemoryReaderService osuMemoryReaderService,
    ISettingsService settingsService,
    ISongLibraryService songLibraryService,
    ISukiDialogManager dialogManager,
    ISukiToastManager toastManager) : ViewModelBase
{
    [ObservableProperty] private SelectedMap _originBeatmap = new();
    [ObservableProperty] private bool _sliderTrackOverride;
    [ObservableProperty] private bool _sliderBorderOverride;
    [ObservableProperty] private bool _overwriteBackground;
    [ObservableProperty] private bool _overwriteVideo;
    [ObservableProperty] private bool _resetBeatmapIds;
    [ObservableProperty] private bool _removeDuplicateTags;
    [ObservableProperty] private bool _hasMultiple;
    [ObservableProperty] private bool _applyMetadataSection = true;
    [ObservableProperty] private bool _applyGeneralSection;
    [ObservableProperty] private bool _applyColoursSection;
    [ObservableProperty] private bool _applyCombosSection;

    [NotifyPropertyChangedFor(nameof(HasHeaderBackgroundImage))]
    [ObservableProperty] private Bitmap? _headerBackgroundImage;

    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originArtist = string.Empty;

    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originSongName = string.Empty;

    [ObservableProperty] private string _backgroundImagePath = string.Empty;

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))]
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [];

    public ObservableCollection<SelectedMap> AdditionalBeatmaps
    {
        get => new ObservableCollection<SelectedMap>(DestinationBeatmaps.Skip(1));
        set
        {
            var first = DestinationBeatmaps.FirstOrDefault() ?? new SelectedMap();
            DestinationBeatmaps =
                new ObservableCollection<SelectedMap>(new[] { first }.Concat(value));
        }
    }

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

    [ObservableProperty] private string _preferredDirectory = string.Empty;
    [ObservableProperty] private AvaloniaBeatmapMetadata _metadata = new();

    [RelayCommand]
    private void RemoveMap(string path)
    {
        DestinationBeatmaps = new ObservableCollection<SelectedMap>(DestinationBeatmaps.Where(x => x.Path != path));
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    [RelayCommand]
    private void ToggleDestinationMap(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (DestinationBeatmaps.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            RemoveMap(path);
            return;
        }

        if (!BeatmapSelectionUtils.TryAppendDestinationBeatmap(DestinationBeatmaps, path, out var destinationBeatmap))
        {
            return;
        }

        DestinationBeatmaps = destinationBeatmap;
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    private async Task ImportMetadataFromOriginAsync(CancellationToken token)
    {
        var origin = OriginBeatmap.Path;
        if (string.IsNullOrEmpty(origin))
        {
            toastManager.ShowToast(NotificationType.Error, "Import Error", "Please select an origin beatmap!");
            return;
        }

        try
        {
            var originMetadata = Beatmap.Decode(await File.ReadAllTextAsync(origin, token) ?? string.Empty);
            var comboColourList = new AvaloniaList<AvaloniaComboColour>();

            if (originMetadata.Colours is not null)
            {
                foreach (var combo in originMetadata.Colours.Combos)
                {
                    comboColourList.Add(new AvaloniaComboColour((int)combo.Number, combo.Colour));
                }
            }

            Metadata = new AvaloniaBeatmapMetadata
            {
                Title = originMetadata.MetadataSection.TitleUnicode,
                RomanizedTitle = originMetadata.MetadataSection.Title,
                Artist = originMetadata.MetadataSection.ArtistUnicode,
                RomanizedArtist = originMetadata.MetadataSection.Artist,
                Creator = originMetadata.MetadataSection.Creator,
                Source = originMetadata.MetadataSection.Source,
                Tags = string.Join(" ", originMetadata.MetadataSection.Tags),
                BeatmapSetId = originMetadata.MetadataSection.BeatmapSetID,
                AudioFilename = originMetadata.GeneralSection.AudioFilename,
                BackgroundFilename = originMetadata.GetBgFilename() ?? string.Empty,
                VideoFilename =
                    originMetadata.Events.EventList.Where(x => x.Type == EventType.Video).Select(x => x as Video)
                        .FirstOrDefault()?.FilePath ?? string.Empty,
                VideoOffset = originMetadata.Events.EventList.Where(x => x.Type == EventType.Video)
                    .Select(x => x as Video).FirstOrDefault()?.StartTime.Milliseconds ?? 0,
                Colours = comboColourList,
                SliderTrackColour = originMetadata.Colours?.SliderTrackOverride,
                SliderBorderColour = originMetadata.Colours?.SliderBorder,
                PreviewTime = originMetadata.GeneralSection.PreviewTime ?? -1,
                WidescreenStoryboard = originMetadata.GeneralSection.WidescreenStoryboard ?? false,
                LetterboxInBreaks = originMetadata.GeneralSection.LetterboxInBreaks ?? false,
                EpilepsyWarning = originMetadata.GeneralSection.EpilepsyWarning ?? false,
                SamplesMatch = originMetadata.GeneralSection.SamplesMatchPlaybackRate ?? false
            };

            SliderBorderOverride = Metadata.SliderBorderColour != null;
            SliderTrackOverride = Metadata.SliderTrackColour != null;
            LoadOriginBeatmapHeader(originMetadata);
            LoadBackgroundImage(origin, originMetadata);

            toastManager.ShowToast(NotificationType.Success, "Import Success", "Successfully imported metadata!");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            ClearOriginBeatmapHeader();
            toastManager.ShowToast(NotificationType.Error, "Import Error", "Failed to import metadata!");
        }
    }

    [RelayCommand]
    private async Task SetOriginFromMemory(CancellationToken token)
    {
        var currentBeatmap = GetBeatmapFromMemory();
        if (currentBeatmap is null)
        {
            return;
        }

        await SetOriginBeatmapPath(currentBeatmap, token);
    }

    [RelayCommand]
    private async Task ReimportOriginMetadataAfterPathChange(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            ClearOriginBeatmapHeader();
            return;
        }

        await ImportMetadataFromOriginAsync(token);
    }

    [RelayCommand]
    private void AddDestinationFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();
        if (currentBeatmap is null)
        {
            return;
        }

        if (!BeatmapSelectionUtils.TryAppendDestinationBeatmap(DestinationBeatmaps, currentBeatmap, out var destinationBeatmap))
        {
            toastManager.ShowToast(NotificationType.Error, "Duplicate Beatmap", "This beatmap is already in the list.");
            return;
        }

        DestinationBeatmaps = destinationBeatmap;
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    private string? GetBeatmapFromMemory()
    {
        return BeatmapSelectionUtils.TryGetBeatmapFromMemory(
            osuMemoryReaderService,
            (type, title, message) => toastManager.ShowToast(type, title, message),
            "Memory Error",
            "Failed to get beatmap from memory.",
            "No Beatmap",
            "No beatmap is currently loaded.");
    }

    [RelayCommand]
    private void RemoveColour(AvaloniaComboColour colour)
    {
        try
        {
            var index = Metadata.Colours.IndexOf(colour);
            Metadata.Colours.RemoveAt(index);

            for (var i = index; i < Metadata.Colours.Count; i++)
            {
                Metadata.Colours[i].Number = i + 1;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
    }

    [RelayCommand]
    private void AddColour()
    {
        Metadata.Colours.Add(new AvaloniaComboColour(Metadata.Colours.Count + 1, Color.White));
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

            await SetOriginBeatmapPath(selectedPaths[0], token);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
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
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
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
            "Metadata Manager",
            string.IsNullOrWhiteSpace(errorMessage)
                ? "Unable to open the origin beatmap folder."
                : errorMessage);
    }

    [RelayCommand]
    private void AddMapsetDiffsToDestination()
    {
        var referencePath = ResolveMapsetReferenceBeatmapPath();
        if (referencePath is null)
        {
            toastManager.ShowToast(
                NotificationType.Warning,
                "Metadata Manager",
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
                "Metadata Manager",
                "No additional mapset difficulties were available to add.");
            return;
        }

        DestinationBeatmaps = updatedDestinationBeatmaps;
        HasMultiple = DestinationBeatmaps.Count > 1;
        toastManager.ShowToast(
            NotificationType.Success,
            "Metadata Manager",
            $"Added {addedCount} mapset diff(s) to destination.");
    }

    private async Task SetOriginBeatmapPath(string beatmapPath, CancellationToken token)
    {
        OriginBeatmap = new SelectedMap
        {
            Path = beatmapPath
        };

        PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? string.Empty;
        await ImportMetadataFromOriginAsync(token);
    }

    private void SetDestinationBeatmaps(IReadOnlyCollection<string> beatmapPaths)
    {
        var normalizedBeatmaps = BeatmapSelectionUtils.NormalizeDestinationBeatmaps(beatmapPaths);
        if (normalizedBeatmaps.Count == 0)
        {
            return;
        }

        DestinationBeatmaps = normalizedBeatmaps;
        HasMultiple = DestinationBeatmaps.Count > 1;
        PreferredDirectory = BeatmapSelectionUtils.GetPreferredDirectoryOrFallback(DestinationBeatmaps, PreferredDirectory);
    }

    private string? ResolveMapsetReferenceBeatmapPath()
    {
        var destinationPaths = DestinationBeatmaps
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

        return !string.IsNullOrWhiteSpace(OriginBeatmap.Path) ? OriginBeatmap.Path : null;
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
            "Metadata Manager",
            allowMultiple,
            token,
            preferredMapsetDirectoryPath);

    private void LoadOriginBeatmapHeader(Beatmap beatmap)
    {
        var metadata = beatmap.MetadataSection;
        OriginArtist = StringValueUtils.FirstNonEmpty(metadata.ArtistUnicode, metadata.Artist);
        OriginSongName = StringValueUtils.FirstNonEmpty(metadata.TitleUnicode, metadata.Title);
    }

    private void LoadBackgroundImage(string beatmapPath, Beatmap beatmap)
    {
        var backgroundRelativePath = beatmap.Events.EventList
            .OfType<Background>()
            .Select(background => background.Filename)
            .FirstOrDefault(filename => !string.IsNullOrWhiteSpace(filename));

        var resolvedBackgroundPath = MapsetAssetPathUtils.ResolveRelativePathFromBeatmap(beatmapPath, backgroundRelativePath);
        BackgroundImagePath = resolvedBackgroundPath ?? string.Empty;

        HeaderBackgroundImage?.Dispose();
        HeaderBackgroundImage = null;

        if (!string.IsNullOrWhiteSpace(resolvedBackgroundPath) && File.Exists(resolvedBackgroundPath))
        {
            HeaderBackgroundImage = new Bitmap(resolvedBackgroundPath);
        }
    }

    private void ClearOriginBeatmapHeader()
    {
        OriginArtist = string.Empty;
        OriginSongName = string.Empty;
        BackgroundImagePath = string.Empty;

        HeaderBackgroundImage?.Dispose();
        HeaderBackgroundImage = null;
    }

    [RelayCommand]
    private void ApplyMetadata()
    {
        var message = "Error occurred while exporting metadata!";
        var type = NotificationType.Warning;
        var error = false;

        var destinationPaths = DestinationBeatmaps
            .Select(x => x.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (string.IsNullOrEmpty(OriginBeatmap.Path))
        {
            message = "Please select an origin beatmap!";
            type = NotificationType.Error;
            error = true;
        }
        else if (destinationPaths.Length == 0)
        {
            message = "Please select at least one destination beatmap!";
            type = NotificationType.Error;
            error = true;
        }

        var appliedMetadata = Metadata;

        if (RemoveDuplicateTags)
        {
            appliedMetadata.Tags = string.Join(" ", appliedMetadata.Tags.Split(' ').Distinct());
        }

        if (!error)
        {
            try
            {
                var options = new MetadataManagerOptions
                {
                    ApplyMetadataSection = ApplyMetadataSection,
                    ResetBeatmapIds = ResetBeatmapIds,
                    ApplyGeneralSection = ApplyGeneralSection,
                    ApplyColoursSection = ApplyColoursSection,
                    ApplyCombosSection = ApplyCombosSection,
                    OverwriteAudio = true,
                    OverwriteBackground = OverwriteBackground,
                    OverwriteVideo = OverwriteVideo,
                    SliderBorderColour = SliderBorderOverride,
                    SliderTrackColour = SliderTrackOverride
                };

                metadataManagerService.ApplyMetadata(appliedMetadata, destinationPaths, options);
                message = $"Successfully exported metadata to {destinationPaths.Length} beatmap(s)!";
                type = NotificationType.Success;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                message = exception.Message;
                type = NotificationType.Error;
            }
        }

        toastManager.ShowToast(type, "Metadata Export", message);
    }
}
