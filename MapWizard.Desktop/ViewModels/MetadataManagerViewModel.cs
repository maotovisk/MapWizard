using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.BeatmapParser;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using Material.Styles.Controls;
using Material.Styles.Models;

namespace MapWizard.Desktop.ViewModels;
public partial class MetadataManagerViewModel(IFilesService filesService) : ViewModelBase
{
    [ObservableProperty]
    private string _snackbarName = Guid.NewGuid().ToString();
        
    [ObservableProperty]
    private SelectedMap _originBeatmap = new();
    
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
    private BeatmapMetadata _metadata = new();
    
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
            var comboColourList = new AvaloniaList<AvaloniaComboColour>();
            
            if (originMetadata.Colours != null)
            {
                foreach (var combo in originMetadata.Colours.Combos)
                {
                    comboColourList.Add(new AvaloniaComboColour((int)combo.Number, combo.Colour));
                }
            }
        
            
            Metadata = new BeatmapMetadata
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
    void RemoveColour(AvaloniaComboColour colour)
    {
        try
        {
            var index = Metadata.Colours.IndexOf(colour);
            Metadata.Colours.RemoveAt(index);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
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
        else if (DestinationBeatmaps.Count == 0)
        {
            message = "Please select at least one destination beatmap!";
        }
       
        SnackbarHost.Post(
            new SnackbarModel(
                message,
                TimeSpan.FromSeconds(8)),
            SnackbarName,
            DispatcherPriority.Normal);
    }
    
}