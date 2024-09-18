using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Controls.ViewModel;

public partial class BeatmapFileSelectorData : ObservableObject
{
    [NotifyPropertyChangedFor(nameof(AdditionalBeatmapPaths))]
    [ObservableProperty]
    private AvaloniaList<SelectedMap> _beatmapPaths;
    
    public AvaloniaList<SelectedMap> AdditionalBeatmapPaths
    {
        get => new AvaloniaList<SelectedMap>(BeatmapPaths.Skip(1));
        set => BeatmapPaths = new AvaloniaList<SelectedMap>(BeatmapPaths.Take(1).Concat(value));
    }

    [ObservableProperty]
    private bool _allowMany;

    [ObservableProperty] 
    private string _title;
    
    [ObservableProperty]
    private string _preferredDirectory;
    
    public BeatmapFileSelectorData()
    {
        BeatmapPaths = [];
    }
    
    [RelayCommand]
    void RemoveMap(string path)
    {
        BeatmapPaths = new AvaloniaList<SelectedMap>(BeatmapPaths.Where(x => x.Path != path));
    }
    
    [RelayCommand]
    async Task PickFiles(CancellationToken token)
    {
        try
        {
            var filesService = (((App)Application.Current!)?.FilesService) ?? throw new Exception("FilesService is not initialized.");
            var file = await filesService.OpenFileAsync(
                new FilePickerOpenOptions()
                {
                    Title = "Select the origin beatmap file",
                    AllowMultiple = AllowMany,
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

            BeatmapPaths = new AvaloniaList<SelectedMap>(file.Select(f => new SelectedMap {Path = f.Path.LocalPath}));
            Console.WriteLine($"Selected file: {string.Join(", ", BeatmapPaths.Select(x => x.Path))}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}