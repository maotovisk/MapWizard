using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.ViewModels;

public partial class HitsoundCopierViewModel : ViewModelBase
{
    public string Message { get; set; } = "Hitsound Copier View";

    private AvaloniaList<SelectedMap> _originBeatmapPath;

    private AvaloniaList<SelectedMap> _destinationBeatmapPath = [];
    
    public AvaloniaList<SelectedMap> OriginBeatmapPath
    {
        get => _originBeatmapPath;
        set
        {
            if (_originBeatmapPath != value)
            {
                _originBeatmapPath = value;
                OnPropertyChanged(nameof(OriginBeatmapPath));
            }
        }
    }

    public AvaloniaList<SelectedMap> DestinationBeatmapPath
    {
        get => _destinationBeatmapPath;
        set
        {
            if (_destinationBeatmapPath != value)
            {
                _destinationBeatmapPath = value;
                OnPropertyChanged(nameof(DestinationBeatmapPath));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    [RelayCommand]
    void CopyHitsounds()
    {
        Console.WriteLine("Selected origin beatmaps: ");
        
        // for some reason OriginBeatmapPath is not being backpopulated from the BeatmapFileSelector user control
        
        
        foreach(var bm in OriginBeatmapPath)
        {
            Console.WriteLine(bm.Path);
        }
        
        Console.WriteLine("Selected destination beatmaps: ");
        foreach(var bm in DestinationBeatmapPath)
        {
            Console.WriteLine(bm.Path);
        }
    }
}
