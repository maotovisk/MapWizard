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
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views.Dialogs;
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
        if (DestinationBeatmaps.Count < 2)
        {
            HasMultiple = false;
        }
    }

    private async Task ImportMetadataFromOriginAsync(CancellationToken token)
    {
        var origin = OriginBeatmap.Path;
        if (string.IsNullOrEmpty(origin))
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("Import Error")
                .WithContent("Please select an origin beatmap!")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
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
                BeatmapId = originMetadata.MetadataSection.BeatmapID,
                BeatmapSetId = originMetadata.MetadataSection.BeatmapSetID,
                AudioFilename = originMetadata.GeneralSection.AudioFilename,
                BackgroundFilename = originMetadata.Events.GetBackgroundImage() ?? string.Empty,
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

            toastManager.CreateToast()
                .OfType(NotificationType.Success)
                .WithTitle("Import Success")
                .WithContent("Successfully imported metadata!")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            ClearOriginBeatmapHeader();

            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("Import Error")
                .WithContent("Failed to import metadata!")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
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
    private void AddDestinationFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();
        if (currentBeatmap is null)
        {
            return;
        }

        var destinationBeatmap = DestinationBeatmaps;
        if (destinationBeatmap.Count == 0 ||
            (destinationBeatmap.Count == 1 && string.IsNullOrEmpty(destinationBeatmap.First().Path)))
        {
            destinationBeatmap = [];
        }

        if (destinationBeatmap.Any(x => string.Equals(x.Path, currentBeatmap, StringComparison.OrdinalIgnoreCase)))
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

        destinationBeatmap = new ObservableCollection<SelectedMap>(destinationBeatmap.Append(new SelectedMap
        {
            Path = currentBeatmap
        }));

        DestinationBeatmaps = destinationBeatmap;
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    private string? GetBeatmapFromMemory()
    {
        var currentBeatmap = osuMemoryReaderService.GetBeatmapPath();

        if (currentBeatmap.Status == ResultStatus.Error)
        {
            toastManager.CreateToast()
                .OfType(NotificationType.Error)
                .WithTitle("Memory Error")
                .WithContent("Failed to get beatmap from memory.")
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
                .WithContent("No beatmap is currently loaded.")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
            return null;
        }

        return currentBeatmap.Value;
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
            var selectedPaths = await ShowSongSelectDialogAsync(allowMultiple: false, token: token);
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
                preferredMapsetDirectoryPath: GetOriginMapsetDirectoryPath());
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
                    .WithTitle("Metadata Manager")
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

    private void LoadOriginBeatmapHeader(Beatmap beatmap)
    {
        var metadata = beatmap.MetadataSection;
        OriginArtist = FirstNonEmpty(metadata.ArtistUnicode, metadata.Artist);
        OriginSongName = FirstNonEmpty(metadata.TitleUnicode, metadata.Title);
    }

    private void LoadBackgroundImage(string beatmapPath, Beatmap beatmap)
    {
        var backgroundRelativePath = beatmap.Events.EventList
            .OfType<Background>()
            .Select(background => background.Filename)
            .FirstOrDefault(filename => !string.IsNullOrWhiteSpace(filename));

        var resolvedBackgroundPath = ResolveBackgroundPath(beatmapPath, backgroundRelativePath);
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

    private static string? ResolveBackgroundPath(string beatmapPath, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var mapsetDirectory = Path.GetDirectoryName(beatmapPath);
        if (string.IsNullOrWhiteSpace(mapsetDirectory))
        {
            return null;
        }

        var sanitized = relativePath.Trim()
            .Trim('"')
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        var candidate = Path.IsPathRooted(sanitized)
            ? sanitized
            : Path.Combine(mapsetDirectory, sanitized);

        return File.Exists(candidate) ? candidate : null;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
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

        if (ResetBeatmapIds)
        {
            appliedMetadata.BeatmapId = 0;
            appliedMetadata.BeatmapSetId = -1;
        }

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

        toastManager.CreateToast()
            .OfType(type)
            .WithTitle("Metadata Export")
            .WithContent(message)
            .Dismiss().ByClicking()
            .Dismiss().After(TimeSpan.FromSeconds(8))
            .Queue();
    }
}
