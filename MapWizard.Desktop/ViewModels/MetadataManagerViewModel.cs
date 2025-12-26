using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BeatmapParser;
using BeatmapParser.Enums.Storyboard;
using BeatmapParser.Events;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Tools.MetadataManager;
using SukiUI.Toasts;
using Color = System.Drawing.Color;

namespace MapWizard.Desktop.ViewModels;

public partial class MetadataManagerViewModel(
    IFilesService filesService,
    IMetadataManagerService metadataManagerService,
    IOsuMemoryReaderService osuMemoryReaderService,
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

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))] [ObservableProperty]
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

    [ObservableProperty] private string _preferredDirectory = "";

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

    [RelayCommand]
    async Task ImportMetadata(CancellationToken token)
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
            AvaloniaList<AvaloniaComboColour> comboColourList = [];

            if (originMetadata.Colours != null)
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
                SamplesMatch = originMetadata.GeneralSection.SamplesMatchPlaybackRate ?? false,
            };
            SliderBorderOverride = Metadata.SliderBorderColour != null;
            SliderTrackOverride = Metadata.SliderTrackColour != null;

            toastManager.CreateToast()
                .OfType(NotificationType.Success)
                .WithTitle("Import Success")
                .WithContent("Successfully imported metadata!")
                .Dismiss().ByClicking()
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);

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
    void SetOriginFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();

        if (currentBeatmap is null) return;

        OriginBeatmap = new SelectedMap()
        {
            Path = currentBeatmap
        };

        PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? "";
    }

    [RelayCommand]
    void AddDestinationFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();

        if (currentBeatmap is null) return;

        var destinationBeatmap = DestinationBeatmaps;

        if (destinationBeatmap.Count == 0 ||
            (destinationBeatmap.Count == 1 && string.IsNullOrEmpty(destinationBeatmap.First().Path)))
        {
            destinationBeatmap = [];
        }

        if (destinationBeatmap.Any(x => x.Path == currentBeatmap))
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

        destinationBeatmap = new ObservableCollection<SelectedMap>(destinationBeatmap.Append(new SelectedMap()
        {
            Path = currentBeatmap
        }));

        if (destinationBeatmap.Count > 1)
        {
            HasMultiple = true;
        }

        DestinationBeatmaps = destinationBeatmap;
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
    void RemoveColour(AvaloniaComboColour colour)
    {
        try
        {
            var index = Metadata.Colours.IndexOf(colour);
            Metadata.Colours.RemoveAt(index);

            // Reprocess the combo numbers

            for (var i = index; i < Metadata.Colours.Count; i++)
            {
                Metadata.Colours[i].Number = i + 1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [RelayCommand]
    void AddColour()
    {
        Metadata.Colours.Add(new AvaloniaComboColour(Metadata.Colours.Count + 1, Color.White));
    }

    [RelayCommand]
    async Task PickOriginFile(CancellationToken token)
    {
        try
        {
            var file = await filesService.OpenFileAsync(
                new FilePickerOpenOptions()
                {
                    Title = "Select the origin beatmap file",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("osu! beatmap file")
                        {
                            Patterns = ["*.osu"],
                            MimeTypes = new List<string>
                            {
                                "application/octet-stream",
                            }
                        }
                    ]
                });

            if (file is null || file.Count == 0) return;

            OriginBeatmap = new SelectedMap()
            {
                Path = file.First().Path.LocalPath
            };

            PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? "";
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [RelayCommand]
    async Task PickDestinationFile(CancellationToken token)
    {
        try
        {
            var preferredDirectory = await filesService.TryGetFolderFromPathAsync(PreferredDirectory);
            var file = await filesService.OpenFileAsync(
                new FilePickerOpenOptions()
                {
                    Title = "Select the origin beatmap file",
                    AllowMultiple = true,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("osu! beatmap file")
                        {
                            Patterns = ["*.osu"],
                            MimeTypes = new List<string>()
                            {
                                "application/octet-stream",
                            }
                        }
                    ],
                    SuggestedStartLocation = preferredDirectory,
                });

            if (file is null || file.Count == 0) return;

            if (file.Count > 1)
            {
                HasMultiple = true;
            }

            DestinationBeatmaps =
                new ObservableCollection<SelectedMap>(file.Select(f => new SelectedMap { Path = f.Path.LocalPath }));
            Console.WriteLine($"Selected file: {string.Join(", ", DestinationBeatmaps.Select(x => x.Path))}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [RelayCommand]
    private void ApplyMetadata()
    {
        var message = "Error occurred while exporting metadata!";
        var type = NotificationType.Warning;
        var error = false;

        if (string.IsNullOrEmpty(OriginBeatmap.Path))
        {
            message = "Please select an origin beatmap!";
            type = NotificationType.Error;
            error = true;
        }
        else if (DestinationBeatmaps.Count == 0 || DestinationBeatmaps.All(x => string.IsNullOrEmpty(x.Path)))
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
            try
            {
                var options = new MetadataManagerOptions()
                {
                    OverwriteAudio = true,
                    OverwriteBackground = OverwriteBackground,
                    OverwriteVideo = OverwriteVideo,
                    SliderBorderColour = SliderBorderOverride,
                    SliderTrackColour = SliderTrackOverride,
                };

                metadataManagerService.ApplyMetadata(appliedMetadata, DestinationBeatmaps.Select(x => x.Path).ToArray(),
                    options);
                message = $"Successfully exported metadata to {DestinationBeatmaps.Count} beatmap(s)!";
                type = NotificationType.Success;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                message = e.Message;
                type = NotificationType.Error;
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
