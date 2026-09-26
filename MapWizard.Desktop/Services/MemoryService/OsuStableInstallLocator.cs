using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MapWizard.Desktop.Services.MemoryService;

/// <summary>
/// Resolves the Songs folder of the currently running osu!stable process, both natively on Windows
/// and under Wine on Linux (where the Windows-side paths have to be mapped through the Wine prefix).
/// </summary>
internal static class OsuStableInstallLocator
{
    /// <summary>
    /// Process name as seen by <see cref="Process.GetProcessesByName(string)"/>. Wine names the
    /// process after the full executable file name, so the extension is kept on Linux.
    /// </summary>
    public static string ProcessName => OperatingSystem.IsWindows() ? "osu!" : "osu!.exe";

    public static bool IsRunning()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        foreach (var process in processes)
        {
            process.Dispose();
        }

        return processes.Length > 0;
    }

    public static string? TryGetRunningSongsFolder()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        try
        {
            if (processes.Length == 0)
            {
                return null;
            }

            var process = processes[0];
            if (OperatingSystem.IsWindows())
            {
                var installPath = Path.GetDirectoryName(process.MainModule?.FileName);
                return string.IsNullOrWhiteSpace(installPath) ? null : ResolveSongsFolder(installPath, winePrefix: null);
            }

            if (OperatingSystem.IsLinux())
            {
                var winePrefix = TryReadWinePrefix(process.Id);
                var installPath = TryGetWineInstallPath(process.Id, winePrefix);
                return installPath is null ? null : ResolveSongsFolder(installPath, winePrefix);
            }

            return null;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static string? ResolveSongsFolder(string installPath, string? winePrefix)
    {
        var beatmapDirectory = TryReadBeatmapDirectory(installPath);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            return ResolveCaseInsensitive(installPath, "Songs");
        }

        if (OperatingSystem.IsWindows())
        {
            var songsPath = Path.IsPathRooted(beatmapDirectory)
                ? beatmapDirectory
                : Path.Combine(installPath, beatmapDirectory);
            return Directory.Exists(songsPath) ? Path.GetFullPath(songsPath) : null;
        }

        return IsWindowsDrivePath(beatmapDirectory)
            ? TryMapWinePath(beatmapDirectory, winePrefix)
            : ResolveCaseInsensitive(installPath, beatmapDirectory);
    }

    private static string? TryReadBeatmapDirectory(string installPath)
    {
        foreach (var configPath in EnumerateUserConfigs(installPath))
        {
            try
            {
                foreach (var line in File.ReadLines(configPath))
                {
                    if (!line.StartsWith("BeatmapDirectory", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf('=');
                    return separatorIndex < 0 ? null : line[(separatorIndex + 1)..].Trim().Trim('"');
                }
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            }
        }

        return null;
    }

    /// <summary>
    /// Yields the current user's config first (Wine uses the Unix user name as the Windows one),
    /// then any other per-user config in the install folder.
    /// </summary>
    private static IEnumerable<string> EnumerateUserConfigs(string installPath)
    {
        var preferred = Path.Combine(installPath, $"osu!.{Environment.UserName}.cfg");
        if (File.Exists(preferred))
        {
            yield return preferred;
        }

        IEnumerable<string> others;
        try
        {
            others = Directory.EnumerateFiles(installPath, "osu!.*.cfg", SearchOption.TopDirectoryOnly).ToList();
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            yield break;
        }

        foreach (var config in others)
        {
            if (!string.Equals(config, preferred, StringComparison.Ordinal))
            {
                yield return config;
            }
        }
    }

    /// <summary>
    /// Wine maps the executable image from its Unix path, so <c>/proc/pid/maps</c> is the most direct
    /// source. Falls back to translating the Windows-style <c>argv[0]</c> through the Wine prefix.
    /// </summary>
    private static string? TryGetWineInstallPath(int pid, string? winePrefix)
    {
        try
        {
            foreach (var line in File.ReadLines($"/proc/{pid}/maps"))
            {
                var pathStart = line.IndexOf('/');
                if (pathStart < 0 || !line.EndsWith("/osu!.exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var installPath = Path.GetDirectoryName(line[pathStart..]);
                if (Directory.Exists(installPath))
                {
                    return installPath;
                }
            }
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
        }

        try
        {
            var executable = File.ReadAllText($"/proc/{pid}/cmdline").Split('\0')[0];
            var executablePath = IsWindowsDrivePath(executable) ? TryMapWinePath(executable, winePrefix) : executable;
            var installPath = Path.GetDirectoryName(executablePath);
            return Directory.Exists(installPath) ? installPath : null;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
    }

    private static string? TryReadWinePrefix(int pid)
    {
        try
        {
            const string key = "WINEPREFIX=";
            var entry = File.ReadAllText($"/proc/{pid}/environ")
                .Split('\0')
                .FirstOrDefault(variable => variable.StartsWith(key, StringComparison.Ordinal));
            if (entry is not null)
            {
                return entry[key.Length..];
            }
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
        }

        var defaultPrefix = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wine");
        return Directory.Exists(defaultPrefix) ? defaultPrefix : null;
    }

    private static bool IsWindowsDrivePath(string path) =>
        path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    /// <summary>
    /// Translates <c>X:\some\path</c> to its Unix location via <c>$WINEPREFIX/dosdevices/x:</c>.
    /// </summary>
    private static string? TryMapWinePath(string windowsPath, string? winePrefix)
    {
        if (winePrefix is null)
        {
            return null;
        }

        var drivePath = Path.Combine(winePrefix, "dosdevices", $"{char.ToLowerInvariant(windowsPath[0])}:");
        if (!Directory.Exists(drivePath))
        {
            return null;
        }

        var driveRoot = new DirectoryInfo(drivePath).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? drivePath;
        return ResolveCaseInsensitive(driveRoot, windowsPath[2..]);
    }

    /// <summary>
    /// Combines <paramref name="root"/> with a Windows-style relative path, matching each segment
    /// case-insensitively like Wine does. Returns <see langword="null"/> when a segment is missing.
    /// </summary>
    private static string? ResolveCaseInsensitive(string root, string relativePath)
    {
        var current = root;
        foreach (var segment in relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries))
        {
            var exact = Path.Combine(current, segment);
            if (Path.Exists(exact))
            {
                current = exact;
                continue;
            }

            try
            {
                var match = new DirectoryInfo(current).EnumerateFileSystemInfos()
                    .FirstOrDefault(entry => string.Equals(entry.Name, segment, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    return null;
                }

                current = match.FullName;
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                return null;
            }
        }

        return Path.Exists(current) ? Path.GetFullPath(current) : null;
    }
}
