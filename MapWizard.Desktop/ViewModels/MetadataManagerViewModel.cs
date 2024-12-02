using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views;
using MapWizard.Tools.HitSoundCopier;
using Material.Styles.Controls;
using Material.Styles.Models;

namespace MapWizard.Desktop.ViewModels;
public partial class MetadataManagerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _snackbarName = Guid.NewGuid().ToString();
    
    private readonly FilesService _filesService = (((App)Application.Current!)?.FilesService) ?? new FilesService((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
        
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
            var file = await _filesService.OpenFileAsync(
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
            var preferredDirectory = await _filesService.TryGetFolderFromPath(PreferredDirectory);
            var file = await _filesService.OpenFileAsync(
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