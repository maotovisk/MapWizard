using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace MapWizard.Desktop.Services;

public interface IFilesService
{
    Task<List<IStorageFile>?> OpenFileAsync(FilePickerOpenOptions? options);
    Task<List<IStorageFolder>> OpenFolderAsync(FolderPickerOpenOptions? options = null);
    Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null);
    Task<IStorageFolder?> TryGetFolderFromPathAsync(string path);
}
