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

namespace MapWizard.Desktop.Controls.ViewModel;

public partial class BeatmapFileSelectorData : ObservableObject
{
    [ObservableProperty]
    private AvaloniaList<string> _beatmapPaths;

    [ObservableProperty]
    private bool _allowMany;

    public BeatmapFileSelectorData()
    {
        BeatmapPaths = [string.Empty];
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

            // Do something with the file
            BeatmapPaths = new AvaloniaList<string>(file.Select(f => f.Path.LocalPath));
            Console.WriteLine($"Selected file: {string.Join(", ", BeatmapPaths)}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}