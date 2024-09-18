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

namespace MapWizard.Desktop.ViewModels;

public partial class HitsoundCopierViewModel : ViewModelBase
{
    public string Message { get; set; }

    [ObservableProperty]
    private AvaloniaList<SelectedMap> _originBeatmapPath = [];

    [ObservableProperty]
    private AvaloniaList<SelectedMap> _destinationBeatmapPath = [];
    
    [RelayCommand]
    void CopyHitsounds()
    {
        Console.WriteLine("Selected origin beatmaps: ");
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

    public HitsoundCopierViewModel()
    {
        Message = "Hitsound Copier View";
    }
}
