using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.ViewModels;

public partial class HitsoundCopierViewModel : ViewModelBase
{
    private IHitSoundService _hitsoundService = new HitSoundService();
    
    public string Message { get; set; } = "Hitsound Copier View";
    [ObservableProperty]
    private SelectedMap _originBeatmap;
    
    [ObservableProperty]
    private bool _hasMultiple = false;

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))] 
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps;
    
    public ObservableCollection<SelectedMap> AdditionalBeatmaps {
        get => new ObservableCollection<SelectedMap>(DestinationBeatmaps.Skip(1));
        set {
            DestinationBeatmaps = new ObservableCollection<SelectedMap>(new[] { DestinationBeatmaps.First() }.Concat(value));
        }
    }
    
    [ObservableProperty]
    private string _preferredDirectory = "";
    
    [RelayCommand]
    private void RemoveMap(string path)
    {
        DestinationBeatmaps = new ObservableCollection<SelectedMap>(DestinationBeatmaps.Where(x => x.Path != path));
    }
    
    [RelayCommand]
    async Task PickOriginFile(CancellationToken token)
    {
        try
        {
            var filesService = (((App)Application.Current!)?.FilesService) ?? throw new Exception("FilesService is not initialized.");
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
                            MimeTypes = new List<string>()
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
            var filesService = (((App)Application.Current!)?.FilesService) ?? throw new Exception("FilesService is not initialized.");
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
                    ]
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
    void CopyHitsounds()
    {
        Console.WriteLine("Copying hitsounds...");

        if (_hitsoundService.CopyHitsoundsAsync(OriginBeatmap.Path, DestinationBeatmaps.Select(x=> x.Path).ToArray()))
        {
            
        }
    }
}
