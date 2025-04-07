using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.BeatmapParser;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Tools.MetadataManager;
using Material.Styles.Controls;
using Material.Styles.Models;
using Color = System.Drawing.Color;

namespace MapWizard.Desktop.ViewModels;
public partial class MetadataManagerViewModel(IFilesService filesService, IMetadataManagerService metadataManagerService, IOsuMemoryReaderService osuMemoryReaderService) : ViewModelBase
{
    [ObservableProperty] private string _snackbarName = "SnackbarMainWindow";
        
    [ObservableProperty]
    private SelectedMap _originBeatmap = new();

    [ObservableProperty]
    private bool _sliderTrackOverride;
    
    [ObservableProperty]
    private bool _sliderBorderOverride;
    
    [ObservableProperty]
    private bool _overwriteBackground;

    [ObservableProperty]
    private bool _overwriteVideo;
    
    [ObservableProperty]
    private bool _resetBeatmapIds;
    
    [ObservableProperty]
    private bool _removeDuplicateTags;
    
    [ObservableProperty]
    private bool _hasMultiple;

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))] 
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [new SelectedMap()];
    
    public ObservableCollection<SelectedMap> AdditionalBeatmaps {
        get => new ObservableCollection<SelectedMap>(DestinationBeatmaps.Skip(1));
        set {
            DestinationBeatmaps = new ObservableCollection<SelectedMap>(new[] { DestinationBeatmaps.First() }.Concat(value));
        }
    }
    
    [ObservableProperty]
    private string _preferredDirectory = "";
    
    [ObservableProperty]
    private AvaloniaBeatmapMetadata _metadata = new();
    
    [RelayCommand]
    private void RemoveMap(string path)
    {
        DestinationBeatmaps = new ObservableCollection<SelectedMap>(DestinationBeatmaps.Where(x => x.Path != path));
    }
    
    [RelayCommand]
    async Task ImportMetadata(CancellationToken token)
    {
        var origin = OriginBeatmap.Path;
        
        if (string.IsNullOrEmpty(origin))
        {
            SnackbarHost.Post(
                new SnackbarModel(
                    "Please select an origin beatmap!",
                    TimeSpan.FromSeconds(8)),
                SnackbarName,
                DispatcherPriority.Normal);
            return;
        }

        try
        {
            var originMetadata = Beatmap.Decode( await File.ReadAllTextAsync(origin, token) ?? string.Empty);
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
                Title = originMetadata.Metadata.TitleUnicode,
                RomanizedTitle = originMetadata.Metadata.Title,
                Artist = originMetadata.Metadata.ArtistUnicode,
                RomanizedArtist = originMetadata.Metadata.Artist,
                Creator = originMetadata.Metadata.Creator,
                Source = originMetadata.Metadata.Source,
                Tags = string.Join(" ", originMetadata.Metadata.Tags),
                BeatmapId = originMetadata.Metadata.BeatmapID,
                BeatmapSetId = originMetadata.Metadata.BeatmapSetID,
                AudioFilename = originMetadata.General.AudioFilename,
                BackgroundFilename = originMetadata.Events.GetBackgroundImage() ?? string.Empty,
                VideoFilename = originMetadata.Events.EventList.Where(x => x.Type == EventType.Video).Select(x=> x as Video).FirstOrDefault()?.FilePath ?? string.Empty,
                VideoOffset = originMetadata.Events.EventList.Where(x => x.Type == EventType.Video).Select(x=> x as Video).FirstOrDefault()?.StartTime.Milliseconds ?? 0,
                Colours = comboColourList,
                SliderTrackColour = originMetadata.Colours?.SliderTrackOverride,
                SliderBorderColour = originMetadata.Colours?.SliderBorder,
                PreviewTime = originMetadata.General.PreviewTime ?? -1,
                WidescreenStoryboard = originMetadata.General.WidescreenStoryboard ?? false,
                LetterboxInBreaks = originMetadata.General.LetterboxInBreaks ?? false,
                EpilepsyWarning = originMetadata.General.EpilepsyWarning ?? false,
                SamplesMatch = originMetadata.General.SamplesMatchPlaybackRate ?? false,
            };
            SliderBorderOverride = Metadata.SliderBorderColour != null;
            SliderTrackOverride = Metadata.SliderTrackColour != null;
            
            SnackbarHost.Post( 
                new SnackbarModel(
                    "Successfully imported metadata!",
                    TimeSpan.FromSeconds(8)),
                SnackbarName,
                DispatcherPriority.Normal);
            
        } catch (Exception e)
        {
            Console.WriteLine(e.Message);
            
            SnackbarHost.Post(
                new SnackbarModel(
                    "Failed to import metadata!",
                    TimeSpan.FromSeconds(8)),
                SnackbarName,
                DispatcherPriority.Normal);
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

        if (DestinationBeatmaps.Count == 0 ||
            (DestinationBeatmaps.Count == 1 && string.IsNullOrEmpty(DestinationBeatmaps.First().Path)))
        {
            DestinationBeatmaps = [];
        }

        if (DestinationBeatmaps.Any(x => x.Path == currentBeatmap))
        {
            SnackbarHost.Post(
                new SnackbarModel(
                    "This beatmap is already in the list.",
                    TimeSpan.FromSeconds(8)),
                SnackbarName,
                DispatcherPriority.Normal);
            return;
        }

        DestinationBeatmaps = new ObservableCollection<SelectedMap>(DestinationBeatmaps.Append(new SelectedMap()
        {
            Path = currentBeatmap
        }));

        if (DestinationBeatmaps.Count > 1)
        {
            HasMultiple = true;
        }
    }

    private string? GetBeatmapFromMemory()
    {
        var currentBeatmap = osuMemoryReaderService.GetBeatmapPath();

        if (currentBeatmap.Status == ResultStatus.Error)
        {
            SnackbarHost.Post(
                new SnackbarModel(
                    currentBeatmap.ErrorMessage ?? "Something went wrong while getting the beatmap path from memory.",
                    TimeSpan.FromSeconds(8)),
                SnackbarName,
                DispatcherPriority.Normal);
            return null;
        }

        if (string.IsNullOrEmpty(currentBeatmap.Value))
        {
            SnackbarHost.Post(
                new SnackbarModel(
                    "No beatmap found in memory.",
                    TimeSpan.FromSeconds(8)),
                SnackbarName,
                DispatcherPriority.Normal);
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
                            Patterns =["*.osu"],
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
                            Patterns =["*.osu"],
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

            DestinationBeatmaps = new ObservableCollection<SelectedMap>(file.Select(f => new SelectedMap {Path = f.Path.LocalPath}));
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
        var message = string.Empty;
        
        if (string.IsNullOrEmpty(OriginBeatmap.Path))
        {
            message = "Please select an origin beatmap!";
        }
        else if (DestinationBeatmaps.Count == 0 || DestinationBeatmaps.All(x => string.IsNullOrEmpty(x.Path)))
        {
            message = "Please select at least one destination beatmap!";
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
            
            metadataManagerService.ApplyMetadata(appliedMetadata, DestinationBeatmaps.Select(x => x.Path).ToArray(), options);
            message = $"Successfully exported metadata to {DestinationBeatmaps.Count} beatmap(s)!";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            message = e.Message;
        }
        
        SnackbarHost.Post(
            new SnackbarModel(
                message,
                TimeSpan.FromSeconds(8)),
            SnackbarName,
            DispatcherPriority.Normal);
    }
    
}