using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Tools.HitSoundCopier;
using Material.Styles.Controls;
using Material.Styles.Models;

namespace MapWizard.Desktop.ViewModels;

public partial class HitSoundCopierViewModel(
    IFilesService filesService,
    IHitSoundService hitSoundService,
    IOsuMemoryReaderService osuMemoryReaderService) : ViewModelBase
{
    [ObservableProperty] private string _snackbarName = "SnackbarMainWindow";

    [ObservableProperty] private SelectedMap _originBeatmap = new();

    [ObservableProperty] private bool _hasMultiple;

    [ObservableProperty] private bool _copySampleAndVolumeChanges = true;

    [ObservableProperty] private bool _overwriteMuting;

    [ObservableProperty] private bool _copySliderBodySounds = true;

    [ObservableProperty] private int _leniency = 5;

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
            var preferredDirectory = PreferredDirectory == string.Empty
                ? await filesService.TryGetFolderFromPathAsync(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                : await filesService.TryGetFolderFromPathAsync(PreferredDirectory);
            var file = await filesService.OpenFileAsync(new FilePickerOpenOptions()
            {
                Title = "Select the origin beatmap file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("osu! beatmap file")
                    {
                        Patterns = ["*.osu"],
                        MimeTypes = new List<string>()
                        {
                            "application/octet-stream"
                        }
                    }
                ],
                SuggestedStartLocation = preferredDirectory
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
            var preferredDirectory = PreferredDirectory == string.Empty
                ? await filesService.TryGetFolderFromPathAsync(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                : await filesService.TryGetFolderFromPathAsync(PreferredDirectory);
            var file = await filesService.OpenFileAsync(new FilePickerOpenOptions()
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
                            "application/octet-stream"
                        }
                    }
                ],
                SuggestedStartLocation = preferredDirectory
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
    private void SetOriginFromMemory()
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
    private void AddDestinationFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();

        if (currentBeatmap is null) return;

        var destinationBeatmaps = DestinationBeatmaps;

        if (destinationBeatmaps.Count == 0 ||
            (destinationBeatmaps.Count == 1 && string.IsNullOrEmpty(destinationBeatmaps.First().Path)))
        {
            destinationBeatmaps = [];
        }

        if (destinationBeatmaps.Any(x => x.Path == currentBeatmap))
        {
            SnackbarHost.Post(
                new SnackbarModel(
                    "This beatmap is already in the list.",
                    TimeSpan.FromSeconds(8)),
                SnackbarName,
                DispatcherPriority.Normal);
            return;
        }

        destinationBeatmaps = new ObservableCollection<SelectedMap>(destinationBeatmaps.Append(new SelectedMap()
        {
            Path = currentBeatmap
        }));

        if (destinationBeatmaps.Count > 1)
        {
            HasMultiple = true;
        }
        
        DestinationBeatmaps = destinationBeatmaps;
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
    private void CopyHitSounds()
    {
        var message = string.Empty;

        var options = new HitSoundCopierOptions()
        {
            CopySampleAndVolumeChanges = CopySampleAndVolumeChanges,
            CopySliderBodySounds = CopySliderBodySounds,
            Leniency = Leniency,
            OverwriteMuting = OverwriteMuting
        };

        if (string.IsNullOrEmpty(OriginBeatmap.Path))
        {
            message = "Please select an origin beatmap!";
        }
        else if (DestinationBeatmaps.Count == 0)
        {
            message = "Please select at least one destination beatmap!";
        }
        else if (hitSoundService.CopyHitsoundsAsync(OriginBeatmap.Path,
                     DestinationBeatmaps.Select(x => x.Path).ToArray(), options))
        {
            message = $"HitSounds applied successfully to {DestinationBeatmaps.Count} beatmap(s)!";
        }

        SnackbarHost.Post(
            new SnackbarModel(
                message,
                TimeSpan.FromSeconds(8)),
            SnackbarName,
            DispatcherPriority.Normal);
    }
}