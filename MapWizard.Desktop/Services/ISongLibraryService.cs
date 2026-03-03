using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapWizard.Desktop.Models.SongSelect;

namespace MapWizard.Desktop.Services;

public interface ISongLibraryService
{
    bool IsValidSongsPath(string? songsPath);
    string? TryDetectSongsPath();
    void InvalidateCache(string? songsPath = null);
    Task<IReadOnlyList<string>> GetMapsetDirectoriesAsync(string songsPath, CancellationToken cancellationToken = default);
    Task<SongMapsetInfo?> LoadMapsetAsync(string mapsetDirectoryPath, CancellationToken cancellationToken = default);
}
