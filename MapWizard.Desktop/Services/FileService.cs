using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MapWizard.Desktop.Services;

public class FilesService (Lazy<TopLevel> toplevelLazy) : IFilesService
{
    private TopLevel TopLevel => toplevelLazy.Value; // Resolve TopLevel only when needed
    
    public async Task<List<IStorageFile>?> OpenFileAsync(FilePickerOpenOptions? options)
    {
        var files = await TopLevel.StorageProvider.OpenFilePickerAsync(options ?? new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = true
        });

        return files.Count >= 1 ? files.ToList() : new List<IStorageFile>();
    }

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null)
    {
        return await TopLevel.StorageProvider.SaveFilePickerAsync(options ?? new FilePickerSaveOptions
        {
            Title = "Save File"
        });
    }

    public async Task<IStorageFolder?> TryGetFolderFromPathAsync(string path)
    {
        return await TopLevel.StorageProvider.TryGetFolderFromPathAsync(path);
    }
}