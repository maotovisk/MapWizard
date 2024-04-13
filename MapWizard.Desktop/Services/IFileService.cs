using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace MapWizard.Desktop.Services;

public interface IFilesService
{
    public Task<List<IStorageFile>?> OpenFileAsync(FilePickerOpenOptions? options);
    public Task<IStorageFile?> SaveFileAsync();
}