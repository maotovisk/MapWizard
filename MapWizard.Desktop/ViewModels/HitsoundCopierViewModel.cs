using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace MapWizard.Desktop.ViewModels;

public class HitsoundCopierViewModel : ViewModelBase
{
    public string Message { get; set; }
    
    private string originBeatmapPath = string.Empty;
    public string OriginBeatmapPath { get=> originBeatmapPath; set => this.RaiseAndSetIfChanged(ref originBeatmapPath, value); }

    private string[] destinationBeatmapPath = [string.Empty];
    public string[] DestinationBeatmapPath { get=> destinationBeatmapPath; set => this.RaiseAndSetIfChanged(ref destinationBeatmapPath, value); }

    public HitsoundCopierViewModel()
    {
        SelectOriginBeatmapCommand = ReactiveCommand.CreateFromTask(OpenOriginFile);
        SelectOriginBeatmapCommand = ReactiveCommand.CreateFromTask(OpenDestinationFile);
        Message = "Hitsound Copier View";
    }

    public ReactiveCommand<Unit, Unit> SelectOriginBeatmapCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectDestinationBeatmapCommand { get; }

    private async Task OpenOriginFile(CancellationToken token)
    {
        try
        {
            var filesService = ((App)Application.Current!)?.FilesService;

            if (filesService is null)
            {
                throw new Exception("FilesService is not initialized.");
            }

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
    private async Task OpenDestinationFile(CancellationToken token)
    {
        try
        {
            var filesService = ((App)Application.Current!)?.FilesService;

            if (filesService is null)
            {
                throw new Exception("FilesService is not initialized.");
            }

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
            DestinationBeatmapPath = file.Select(f => f.Path.LocalPath).ToArray();
            
            Console.WriteLine($"Selected file: {OriginBeatmapPath}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
