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

namespace MapWizard.Desktop.ViewModels;

public partial class HitsoundCopierViewModel : ViewModelBase
{
    public string Message { get; set; }

    [ObservableProperty]
    private string _originBeatmapPath = string.Empty;

    [ObservableProperty]
    private AvaloniaList<string> _destinationBeatmapPath = [string.Empty];

    public HitsoundCopierViewModel()
    {
        Message = "Hitsound Copier View";
    }

    [RelayCommand]
    async Task OpenOriginFile(CancellationToken token)
    {
        try
        {
            var filesService = (((App)Application.Current!)?.FilesService) ?? throw new Exception("FilesService is not initialized.");

            var file = await filesService.OpenFileAsync(
                new FilePickerOpenOptions()
                {
                    Title = "Select the origin beatmap file",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("osu! beatmap file")
                        {
                            Patterns =["*.osu"],
                            MimeTypes = new List<string>()
                            {
                                "application/octet-stream",
                            }
                        }
                    }
                });
            if (file is null || file.Count == 0) return;

            // Do something with the file
            OriginBeatmapPath = file[0].Path.LocalPath;

            Console.WriteLine($"Selected file: {OriginBeatmapPath}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [RelayCommand]
    async Task OpenDestinationFile(CancellationToken token)
    {
        try
        {
            var filesService = (((App)Application.Current!)?.FilesService) ?? throw new Exception("FilesService is not initialized.");
            var file = await filesService.OpenFileAsync(
                new FilePickerOpenOptions()
                {
                    Title = "Select the origin beatmap file",
                    AllowMultiple = true,
                    FileTypeFilter = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("osu! beatmap file")
                        {
                            Patterns =["*.osu"],
                            MimeTypes = new List<string>()
                            {
                                "application/octet-stream",
                            }
                        }
                    }
                });

            if (file is null || file.Count == 0) return;

            // Do something with the file
            DestinationBeatmapPath = new AvaloniaList<string>(file.Select(f => f.Path.LocalPath));

            Console.WriteLine($"Selected file: {DestinationBeatmapPath}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
