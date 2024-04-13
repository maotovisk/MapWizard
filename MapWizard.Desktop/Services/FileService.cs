using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MapWizard.Desktop.Services;

public class FilesService(TopLevel target) : IFilesService
{
    public async Task<List<IStorageFile>?> OpenFileAsync(FilePickerOpenOptions? options)
    {
        var files = await target.StorageProvider.OpenFilePickerAsync(options ?? new FilePickerOpenOptions()
        {
            Title = "Open File",
            AllowMultiple = true
        });

        return files.Count >= 1 ? files.ToList() : [];
    }

    public async Task<IStorageFile?> SaveFileAsync()
    {
        return await target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save File"
        });
    }
}