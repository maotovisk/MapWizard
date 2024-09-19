using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Controls;

public partial class BeatmapFileSelector : ContentControl, INotifyPropertyChanged
{
    private AvaloniaList<SelectedMap> _beatmapPaths = [];
    private AvaloniaList<SelectedMap> _additionalBeatmapPaths = [];

    private string _preferredDirectory = "";
    private string _title = "";
    private bool _allowMany;

    public AvaloniaList<SelectedMap> BeatmapPaths
    {
        get => _beatmapPaths;
        set
        {
            SetAndRaise(BeatmapPathsProperty, ref _beatmapPaths, value);
            OnPropertyChanged(nameof(BeatmapPaths));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public string PreferredDirectory
    {
        get => _preferredDirectory;
        set => SetAndRaise(PreferredDirectoryProperty, ref _preferredDirectory, value);
    }
    
    public string Title
    {
        get => _title;
        set => SetAndRaise(TitleProperty, ref _title, value);
    }
    
    public bool AllowMany
    {
        get => _allowMany;
        set => SetAndRaise(AllowManyProperty, ref _allowMany, value);
    }
    
    public AvaloniaList<SelectedMap> AdditionalBeatmapPaths 
    {
        get => new (_beatmapPaths.Skip(1));
        set => SetAndRaise(AdditionalBeatmapPathsProperty, ref _additionalBeatmapPaths, value);
    }
    
    [RelayCommand]
    private void RemoveMap(string path)
    {
        BeatmapPaths = new AvaloniaList<SelectedMap>(BeatmapPaths.Where(x => x.Path != path));
    }
    
    [RelayCommand]
    private async Task PickFiles(CancellationToken token)
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
    // use direct property for the BeatmapPaths
    public static readonly DirectProperty<BeatmapFileSelector, AvaloniaList<SelectedMap>> BeatmapPathsProperty =
        AvaloniaProperty.RegisterDirect<BeatmapFileSelector, AvaloniaList<SelectedMap>>(
            nameof(BeatmapPaths),
            o => o.BeatmapPaths,
            (o, v) => o.BeatmapPaths = v);
    
    public static readonly DirectProperty<BeatmapFileSelector, string> PreferredDirectoryProperty =
        AvaloniaProperty.RegisterDirect<BeatmapFileSelector, string>(
            nameof(PreferredDirectory),
            o => o.PreferredDirectory,
            (o, v) => o.PreferredDirectory = v);
    
    public static readonly DirectProperty<BeatmapFileSelector, string> TitleProperty =
        AvaloniaProperty.RegisterDirect<BeatmapFileSelector, string>(
            nameof(Title),
            o => o.Title,
            (o, v) => o.Title = v);
    
    public static readonly DirectProperty<BeatmapFileSelector, bool> AllowManyProperty =
        AvaloniaProperty.RegisterDirect<BeatmapFileSelector, bool>(
            nameof(AllowMany),
            o => o.AllowMany,
            (o, v) => o.AllowMany = v);
    
    public static readonly DirectProperty<BeatmapFileSelector, AvaloniaList<SelectedMap>> AdditionalBeatmapPathsProperty =
        AvaloniaProperty.RegisterDirect<BeatmapFileSelector, AvaloniaList<SelectedMap>>(
            nameof(AdditionalBeatmapPaths),
            o => o.AdditionalBeatmapPaths,
            (o, v) => o.AdditionalBeatmapPaths = v);
    
    private void UpdateAdditionalBeatmapPaths()
    {
        AdditionalBeatmapPaths = new AvaloniaList<SelectedMap>(BeatmapPaths.Skip(1));
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BeatmapPathsProperty)
        {
            Console.WriteLine($"BeatmapPaths changed: {string.Join(", ", BeatmapPaths.Select(x => x.Path))}");
            SetAndRaise(AdditionalBeatmapPathsProperty, ref _additionalBeatmapPaths, new AvaloniaList<SelectedMap>(BeatmapPaths.Skip(1)));
        }
        
        if (change.Property == AdditionalBeatmapPathsProperty)
        {
            Console.WriteLine($"AdditionalBeatmapPaths changed: {string.Join(", ", AdditionalBeatmapPaths.Select(x => x.Path))}");
            
            _beatmapPaths = new AvaloniaList<SelectedMap>(new [] {BeatmapPaths.First()}.Concat(AdditionalBeatmapPaths));
        }
    }
    
    public BeatmapFileSelector()
    {
        DataContext = this;
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}