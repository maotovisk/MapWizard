using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using BeatmapParser;
using BeatmapParser.Events;
using MapWizard.Desktop.Models.SongSelect;
using Microsoft.Win32;

namespace MapWizard.Desktop.Services;

public sealed class SongLibraryService : ISongLibraryService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(3);
    private readonly SemaphoreSlim _scanGate = new(1, 1);

    private string? _cachedSongsPath;
    private DateTime _cachedAtUtc;
    private IReadOnlyList<string> _cachedMapsetDirectories = [];
    private readonly Dictionary<string, SongMapsetInfo?> _mapsetCache = new(StringComparer.OrdinalIgnoreCase);

    public bool IsValidSongsPath(string? songsPath)
    {
        return !string.IsNullOrWhiteSpace(songsPath) && Directory.Exists(songsPath);
    }

    public string? TryDetectSongsPath()
    {
        var detected = OperatingSystem.IsWindows()
            ? TryDetectSongsPathWindows()
            : OperatingSystem.IsLinux()
                ? TryDetectSongsPathLinux()
                : null;

        return IsValidSongsPath(detected) ? Path.GetFullPath(detected!) : null;
    }

    public async Task<IReadOnlyList<string>> GetMapsetDirectoriesAsync(
        string songsPath,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidSongsPath(songsPath))
        {
            return [];
        }

        var normalizedSongsPath = Path.GetFullPath(songsPath);
        await _scanGate.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(_cachedSongsPath, normalizedSongsPath, StringComparison.OrdinalIgnoreCase) &&
                DateTime.UtcNow - _cachedAtUtc <= CacheLifetime)
            {
                return _cachedMapsetDirectories;
            }

            var directories = await Task.Run(
                () => (IReadOnlyList<string>)EnumerateMapsetDirectories(normalizedSongsPath),
                cancellationToken);

            if (!string.Equals(_cachedSongsPath, normalizedSongsPath, StringComparison.OrdinalIgnoreCase))
            {
                _mapsetCache.Clear();
            }

            _cachedSongsPath = normalizedSongsPath;
            _cachedAtUtc = DateTime.UtcNow;
            _cachedMapsetDirectories = directories;
            return directories;
        }
        finally
        {
            _scanGate.Release();
        }
    }

    public async Task<SongMapsetInfo?> LoadMapsetAsync(
        string mapsetDirectoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mapsetDirectoryPath))
        {
            return null;
        }

        var normalizedPath = Path.GetFullPath(mapsetDirectoryPath);
        if (!Directory.Exists(normalizedPath))
        {
            return null;
        }

        await _scanGate.WaitAsync(cancellationToken);
        try
        {
            if (_mapsetCache.TryGetValue(normalizedPath, out var cachedMapset))
            {
                return cachedMapset;
            }
        }
        finally
        {
            _scanGate.Release();
        }

        var parsedMapset = await Task.Run(
            () => TryBuildMapsetInfo(normalizedPath, cancellationToken),
            cancellationToken);

        await _scanGate.WaitAsync(cancellationToken);
        try
        {
            if (_mapsetCache.TryGetValue(normalizedPath, out var cachedMapset))
            {
                return cachedMapset;
            }

            _mapsetCache[normalizedPath] = parsedMapset;
            return parsedMapset;
        }
        finally
        {
            _scanGate.Release();
        }
    }

    private static List<string> EnumerateMapsetDirectories(string songsPath)
    {
        var mapsetDirectories = new List<(string Path, DateTime LastEditUtc)>();
        IEnumerable<string> directories;

        try
        {
            directories = Directory.EnumerateDirectories(songsPath);
        }
        catch
        {
            return [];
        }

        foreach (var mapsetDirectory in directories)
        {
            try
            {
                var lastWrite = Directory.GetLastWriteTimeUtc(mapsetDirectory);
                mapsetDirectories.Add((mapsetDirectory, lastWrite));
            }
            catch
            {
                // ignored
            }
        }

        return mapsetDirectories
            .OrderByDescending(entry => entry.LastEditUtc)
            .Select(entry => entry.Path)
            .ToList();
    }

    private static SongMapsetInfo? TryBuildMapsetInfo(string mapsetDirectory, CancellationToken cancellationToken)
    {
        string[] osuFiles;
        try
        {
            osuFiles = Directory.EnumerateFiles(mapsetDirectory, "*.osu", SearchOption.TopDirectoryOnly).ToArray();
        }
        catch
        {
            return null;
        }

        if (osuFiles.Length == 0)
        {
            return null;
        }

        var difficulties = new List<SongDifficultyInfo>(osuFiles.Length);
        var artist = string.Empty;
        var title = string.Empty;
        var creator = string.Empty;
        string? backgroundPath = null;
        var mapsetLastEditUtc = DateTime.MinValue;

        foreach (var osuFile in osuFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileLastEditUtc = File.GetLastWriteTimeUtc(osuFile);
            if (fileLastEditUtc > mapsetLastEditUtc)
            {
                mapsetLastEditUtc = fileLastEditUtc;
            }

            Beatmap? beatmap = null;
            try
            {
                beatmap = Beatmap.Decode(new FileInfo(osuFile));
            }
            catch
            {
                // ignored
            }

            if (beatmap is not null && string.IsNullOrWhiteSpace(artist))
            {
                artist = FirstNonEmpty(beatmap.MetadataSection.Artist, beatmap.MetadataSection.ArtistUnicode);
            }

            if (beatmap is not null && string.IsNullOrWhiteSpace(title))
            {
                title = FirstNonEmpty(beatmap.MetadataSection.Title, beatmap.MetadataSection.TitleUnicode);
            }

            if (beatmap is not null && string.IsNullOrWhiteSpace(creator))
            {
                creator = beatmap.MetadataSection.Creator;
            }

            if (beatmap is not null && string.IsNullOrWhiteSpace(backgroundPath))
            {
                var backgroundRelativePath = beatmap.Events.EventList
                    .OfType<Background>()
                    .Select(background => background.Filename)
                    .FirstOrDefault(filename => !string.IsNullOrWhiteSpace(filename));

                if (!string.IsNullOrWhiteSpace(backgroundRelativePath))
                {
                    backgroundPath = ResolveBackgroundPath(mapsetDirectory, backgroundRelativePath);
                }
            }

            var difficultyName = beatmap is null || string.IsNullOrWhiteSpace(beatmap.MetadataSection.Version)
                ? Path.GetFileNameWithoutExtension(osuFile)
                : beatmap.MetadataSection.Version;

            difficulties.Add(new SongDifficultyInfo
            {
                Name = difficultyName,
                OsuFilePath = osuFile,
                LastEditUtc = fileLastEditUtc
            });
        }

        if (difficulties.Count == 0)
        {
            return null;
        }

        difficulties = difficulties
            .OrderByDescending(difficulty => difficulty.LastEditUtc)
            .ToList();

        if (mapsetLastEditUtc == DateTime.MinValue)
        {
            mapsetLastEditUtc = Directory.GetLastWriteTimeUtc(mapsetDirectory);
        }

        return new SongMapsetInfo
        {
            MapsetDirectoryPath = mapsetDirectory,
            Artist = string.IsNullOrWhiteSpace(artist) ? "Unknown Artist" : artist,
            Title = string.IsNullOrWhiteSpace(title) ? "Unknown Title" : title,
            Creator = string.IsNullOrWhiteSpace(creator) ? "Unknown Creator" : creator,
            BackgroundImagePath = backgroundPath,
            LastEditUtc = mapsetLastEditUtc,
            Difficulties = difficulties
        };
    }

    private static string? ResolveBackgroundPath(string mapsetDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var sanitized = relativePath.Trim()
            .Trim('"')
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        var candidate = Path.IsPathRooted(sanitized)
            ? sanitized
            : Path.Combine(mapsetDirectory, sanitized);

        return File.Exists(candidate) ? candidate : null;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    [SupportedOSPlatform("windows")]
    private static string? TryDetectSongsPathWindows()
    {
        foreach (var installPath in EnumerateWindowsInstallPaths())
        {
            var songsPath = ResolveSongsPathFromInstallPath(installPath);
            if (songsPath is not null)
            {
                return songsPath;
            }
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> EnumerateWindowsInstallPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddPath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var trimmed = value.Trim().Trim('"');
            if (File.Exists(trimmed))
            {
                trimmed = Path.GetDirectoryName(trimmed) ?? trimmed;
            }

            if (Directory.Exists(trimmed))
            {
                paths.Add(trimmed);
            }
        }

        AddPath(ReadRegistryValue(@"HKEY_CURRENT_USER\Software\osu!", "path"));
        AddPath(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\Software\osu!", "path"));

        AddPath(ExtractExecutableDirectory(ReadRegistryValue(@"HKEY_CLASSES_ROOT\osu\shell\open\command", null)));
        AddPath(ExtractExecutableDirectory(ReadRegistryValue(@"HKEY_CLASSES_ROOT\osu\DefaultIcon", null)));
        AddPath(ExtractExecutableDirectory(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\Software\Classes\osu\shell\open\command", null)));
        AddPath(ExtractExecutableDirectory(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\Software\Classes\osu\DefaultIcon", null)));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            AddPath(Path.Combine(localAppData, "osu!"));
        }

        foreach (var process in Process.GetProcessesByName("osu!"))
        {
            try
            {
                AddPath(process.MainModule?.FileName);
            }
            catch
            {
                // ignored
            }
            finally
            {
                process.Dispose();
            }
        }

        return paths;
    }

    private static string? TryDetectSongsPathLinux()
    {
        foreach (var installPath in EnumerateLinuxInstallPaths())
        {
            var songsPath = ResolveSongsPathFromInstallPath(installPath);
            if (songsPath is not null)
            {
                return songsPath;
            }
        }

        foreach (var songsDirectory in EnumerateLinuxSongsDirectories())
        {
            if (Directory.Exists(songsDirectory))
            {
                return songsDirectory;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateLinuxInstallPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrWhiteSpace(xdgDataHome))
        {
            xdgDataHome = Path.Combine(home, ".local", "share");
        }

        foreach (var osupathFile in EnumerateLinuxOsuPathFiles(home, xdgDataHome))
        {
            var detectedPathRaw = TryReadFirstLine(osupathFile);
            if (string.IsNullOrWhiteSpace(detectedPathRaw))
            {
                continue;
            }

            var normalizedCandidates = NormalizeLinuxInstallPath(detectedPathRaw, home);
            foreach (var candidatePath in normalizedCandidates)
            {
                if (Directory.Exists(candidatePath))
                {
                    paths.Add(candidatePath);
                }
            }
        }

        var defaultCandidates = new[]
        {
            Path.Combine(home, ".osu"),
            Path.Combine(xdgDataHome, "osuconfig"),
            Path.Combine(home, ".local", "share", "osu-wine", "osu!"),
            Path.Combine(home, ".local", "share", "osu-winello", "osu!"),
            Path.Combine(home, ".wine", "drive_c", "osu!"),
            Path.Combine(home, ".wine", "drive_c", "Program Files", "osu!"),
            Path.Combine(home, ".wine", "drive_c", "Program Files (x86)", "osu!")
        };

        foreach (var candidate in defaultCandidates)
        {
            if (Directory.Exists(candidate))
            {
                paths.Add(candidate);
            }
        }

        return paths;
    }

    private static IEnumerable<string> EnumerateLinuxOsuPathFiles(string home, string xdgDataHome)
    {
        return
        [
            Path.Combine(xdgDataHome, "osuconfig", "osupath"),
            Path.Combine(xdgDataHome, "osuconfig", ".osu-path-winepath"),
            Path.Combine(xdgDataHome, "osuconfig", ".osu-exe-winepath"),
            Path.Combine(home, ".local", "share", "osu-wine", "osupath"),
            Path.Combine(home, ".local", "share", "osu-winello", "osupath"),
            Path.Combine(home, ".config", "osu-wine", "osupath"),
            Path.Combine(home, ".config", "osu-winello", "osupath"),
            Path.Combine(home, ".osu", "osupath")
        ];
    }

    private static IEnumerable<string> NormalizeLinuxInstallPath(string rawPath, string home)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var trimmed = path.Trim().Trim('"').Trim('\'');
            if (trimmed.Length == 0)
            {
                return;
            }

            var normalized = trimmed;
            if (File.Exists(normalized))
            {
                normalized = Path.GetDirectoryName(normalized) ?? normalized;
            }

            if (!Path.IsPathRooted(normalized))
            {
                return;
            }

            if (Directory.Exists(normalized))
            {
                seen.Add(Path.GetFullPath(normalized));
            }
        }

        AddPath(rawPath);

        var trimmedRaw = rawPath.Trim().Trim('"').Trim('\'');
        if (!TryParseWindowsStylePath(trimmedRaw, out var driveLetter, out var relativePath))
        {
            return seen;
        }

        foreach (var unixBasePath in ResolveWineDriveRootCandidates(driveLetter, home))
        {
            if (!Directory.Exists(unixBasePath))
            {
                continue;
            }

            var candidatePath = string.IsNullOrWhiteSpace(relativePath)
                ? unixBasePath
                : Path.Combine(unixBasePath, relativePath);
            AddPath(candidatePath);
        }

        return seen;
    }

    private static bool TryParseWindowsStylePath(string rawPath, out char driveLetter, out string relativePath)
    {
        driveLetter = '\0';
        relativePath = string.Empty;

        if (rawPath.Length < 2 || rawPath[1] != ':')
        {
            return false;
        }

        var letter = rawPath[0];
        if (!char.IsLetter(letter))
        {
            return false;
        }

        driveLetter = char.ToLowerInvariant(letter);

        var suffix = rawPath.Length > 2 ? rawPath[2..] : string.Empty;
        suffix = suffix.TrimStart('\\', '/')
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        relativePath = suffix;
        return true;
    }

    private static IEnumerable<string> ResolveWineDriveRootCandidates(char driveLetter, string home)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine("/mnt", driveLetter.ToString()),
            Path.Combine("/run/media", Environment.UserName, driveLetter.ToString())
        };

        var possiblePrefixes = new[]
        {
            Environment.GetEnvironmentVariable("WINEPREFIX"),
            Path.Combine(home, ".wine"),
            Path.Combine(home, ".local", "share", "wineprefixes", "osu-wineprefix"),
            Path.Combine(home, ".local", "share", "osuconfig", "wine-osu"),
            Path.Combine(home, ".local", "share", "osuconfig", "wine-osu-cachy-10.0")
        };

        foreach (var prefix in possiblePrefixes.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var driveLinkPath = Path.Combine(prefix!, "dosdevices", $"{driveLetter}:");
            try
            {
                if (!Directory.Exists(driveLinkPath))
                {
                    continue;
                }

                var driveInfo = new DirectoryInfo(driveLinkPath);
                if (driveInfo.LinkTarget is null)
                {
                    candidates.Add(driveLinkPath);
                    continue;
                }

                var targetPath = Path.GetFullPath(Path.Combine(driveInfo.Parent?.FullName ?? "/", driveInfo.LinkTarget));
                candidates.Add(targetPath);
            }
            catch
            {
                // ignored
            }
        }

        return candidates;
    }

    private static IEnumerable<string> EnumerateLinuxSongsDirectories()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return
        [
            Path.Combine(home, ".osu", "Songs"),
            Path.Combine(home, ".local", "share", "osu-wine", "Songs"),
            Path.Combine(home, ".local", "share", "osu-winello", "Songs"),
            Path.Combine(home, ".wine", "drive_c", "osu!", "Songs")
        ];
    }

    private static string? ResolveSongsPathFromInstallPath(string installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }

        var normalizedInstallPath = installPath.Trim().Trim('"');
        if (File.Exists(normalizedInstallPath))
        {
            normalizedInstallPath = Path.GetDirectoryName(normalizedInstallPath) ?? normalizedInstallPath;
        }

        if (!Directory.Exists(normalizedInstallPath))
        {
            return null;
        }

        foreach (var configFile in EnumerateOsuConfigFiles(normalizedInstallPath))
        {
            var configuredSongsPath = TryReadBeatmapDirectoryFromConfig(configFile, normalizedInstallPath);
            if (!string.IsNullOrWhiteSpace(configuredSongsPath) && Directory.Exists(configuredSongsPath))
            {
                return Path.GetFullPath(configuredSongsPath);
            }
        }

        var defaultSongsPath = Path.Combine(normalizedInstallPath, "Songs");
        return Directory.Exists(defaultSongsPath) ? Path.GetFullPath(defaultSongsPath) : null;
    }

    private static IEnumerable<string> EnumerateOsuConfigFiles(string osuInstallPath)
    {
        try
        {
            return Directory.EnumerateFiles(osuInstallPath, "osu!.*.cfg", SearchOption.TopDirectoryOnly);
        }
        catch
        {
            return [];
        }
    }

    private static string? TryReadBeatmapDirectoryFromConfig(string configPath, string installPath)
    {
        try
        {
            foreach (var rawLine in File.ReadLines(configPath))
            {
                if (!rawLine.StartsWith("BeatmapDirectory", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var separatorIndex = rawLine.IndexOf('=');
                if (separatorIndex < 0 || separatorIndex + 1 >= rawLine.Length)
                {
                    return null;
                }

                var configuredPath = rawLine[(separatorIndex + 1)..].Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    return null;
                }

                return Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.GetFullPath(Path.Combine(installPath, configuredPath));
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static string? ExtractExecutableDirectory(string? commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return null;
        }

        var trimmed = commandText.Trim();
        string? executablePath;

        if (trimmed.StartsWith('"'))
        {
            var closingQuoteIndex = trimmed.IndexOf('"', 1);
            executablePath = closingQuoteIndex > 1
                ? trimmed[1..closingQuoteIndex]
                : null;
        }
        else
        {
            executablePath = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        executablePath = executablePath.Trim().Trim('"');

        if (executablePath.EndsWith(",0", StringComparison.Ordinal) ||
            executablePath.EndsWith(",1", StringComparison.Ordinal))
        {
            executablePath = executablePath[..^2];
        }

        if (File.Exists(executablePath))
        {
            return Path.GetDirectoryName(executablePath);
        }

        return Directory.Exists(executablePath) ? executablePath : null;
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadRegistryValue(string keyPath, string? valueName)
    {
        try
        {
            return Registry.GetValue(keyPath, valueName, null)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadFirstLine(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using var reader = new StreamReader(filePath);
            return reader.ReadLine();
        }
        catch
        {
            return null;
        }
    }

}
