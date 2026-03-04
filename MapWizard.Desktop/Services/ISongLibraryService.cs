using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapWizard.Desktop.Models.SongSelect;

namespace MapWizard.Desktop.Services;

public interface ISongLibraryService
{
    bool IsValidSongsPath(string? songsPath);
    string? TryDetectSongsPath();

    /// <summary>
    /// Invalidates cached song-library scan and mapset metadata entries.
    /// </summary>
    /// <param name="songsPath">
    /// Optional songs root path to target invalidation. When <see langword="null"/> or whitespace,
    /// all cached entries are invalidated. When provided, invalidation is applied only if it matches
    /// the currently cached songs path.
    /// </param>
    /// <remarks>
    /// Safe to call concurrently with <see cref="GetMapsetDirectoriesAsync"/> and <see cref="LoadMapsetAsync"/>.
    /// </remarks>
    void InvalidateCache(string? songsPath = null);

    Task<IReadOnlyList<string>> GetMapsetDirectoriesAsync(string songsPath, CancellationToken cancellationToken = default);
    Task<SongMapsetInfo?> LoadMapsetAsync(string mapsetDirectoryPath, CancellationToken cancellationToken = default);
}
