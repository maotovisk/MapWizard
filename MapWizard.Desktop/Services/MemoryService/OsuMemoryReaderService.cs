using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Utils;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using ProcessMemoryDataFinder;

namespace MapWizard.Desktop.Services.MemoryService;

public class OsuMemoryReaderService(ISettingsService settingsService, ISongLibraryService songLibraryService)
    : IOsuMemoryReaderService
{
    /// <summary>
    /// How long to wait for the reader's background process watcher to attach on first use.
    /// </summary>
    private static readonly TimeSpan AttachTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The structured reader is a shared instance; background polling and user actions must not
    /// read through it concurrently.
    /// </summary>
    private static readonly Lock ReaderLock = new();

    /// <summary>
    /// Under Wine there is no <c>kernel32</c> to query process bitness, so the check is skipped (the
    /// default options would throw inside the reader's process watcher).
    /// </summary>
    private static ProcessTargetOptions StableProcessTarget { get; } = OperatingSystem.IsWindows()
        ? new ProcessTargetOptions(OsuStableInstallLocator.ProcessName)
        : new ProcessTargetOptions(OsuStableInstallLocator.ProcessName, Target64Bit: null);

    public Result<string> GetBeatmapPath()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            return Error<string>("This feature is not yet supported on your operating system.");
        }

        var memoryResult = GetBeatmapFromMemory();
        if (memoryResult.Status == ResultStatus.Success && !string.IsNullOrWhiteSpace(memoryResult.Value))
        {
            return memoryResult;
        }

        var fallbackIpcResult = GetBeatmapFromFallbackIpc();
        if (fallbackIpcResult.Status == ResultStatus.Success && !string.IsNullOrWhiteSpace(fallbackIpcResult.Value))
        {
            return fallbackIpcResult;
        }

        return Error<string>("Memory read failed and fallback IPC path could not be resolved.");
    }

    public Result<int> GetCurrentTimestamp()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            return Error<int>("Timestamp reading is currently supported only on Windows and Linux.");
        }

        if (FallbackClientIpc.TryReadEditorTime(out var timestamp, out var ipcError))
        {
            return new Result<int>
            {
                Value = timestamp,
                Status = ResultStatus.Success,
                ErrorMessage = null
            };
        }

        return Error<int>($"Unable to read current timestamp from fallback IPC. {ipcError}");
    }

    private Result<string> GetBeatmapFromMemory()
    {
        CurrentBeatmap currentBeatmap;
        try
        {
            if (!OsuStableInstallLocator.IsRunning())
            {
                return Error<string>("osu!stable is not running.");
            }

            lock (ReaderLock)
            {
                var reader = StructuredOsuMemoryReader.GetInstance(StableProcessTarget);
                if (!WaitForAttach(reader))
                {
                    return Error<string>("Unable to attach to the osu! process.");
                }

                currentBeatmap = new CurrentBeatmap();
                if (!reader.TryRead(currentBeatmap))
                {
                    return Error<string>("Unable to read current beatmap.");
                }
            }
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return Error<string>(ex.Message);
        }

        if (string.IsNullOrEmpty(currentBeatmap.OsuFileName) || string.IsNullOrEmpty(currentBeatmap.FolderName))
        {
            return Error<string>("No beatmap is currently loaded.");
        }

        // Prefer the configured/detected Songs folder, but fall back to the one the running client
        // uses in case the settings point at a different install.
        foreach (var songsFolder in EnumerateSongsFolders())
        {
            var beatmapPath = Path.Combine(songsFolder, currentBeatmap.FolderName, currentBeatmap.OsuFileName);
            if (!OperatingSystem.IsWindows())
            {
                beatmapPath = beatmapPath.Replace('\\', '/');
            }

            if (File.Exists(beatmapPath))
            {
                return Success(Path.GetFullPath(beatmapPath));
            }
        }

        return Error<string>("Beatmap file does not exist. Check configured Songs folder in Settings.");
    }

    private static bool WaitForAttach(StructuredOsuMemoryReader reader)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!reader.CanRead)
        {
            if (stopwatch.Elapsed >= AttachTimeout)
            {
                return false;
            }

            Thread.Sleep(25);
        }

        return true;
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    private Result<string> GetBeatmapFromFallbackIpc()
    {
        if (!FallbackClientIpc.TryReadBeatmapPath(out var beatmapPath, out var ipcError))
        {
            return Error<string>(ipcError ?? "Unable to read beatmap path from fallback IPC.");
        }

        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return Error<string>("Fallback IPC returned an empty beatmap path.");
        }

        beatmapPath = beatmapPath.Trim().Trim('"');
        if (File.Exists(beatmapPath))
        {
            return Success(Path.GetFullPath(beatmapPath));
        }

        if (!Path.IsPathRooted(beatmapPath))
        {
            foreach (var songsFolder in EnumerateSongsFolders())
            {
                var candidate = Path.Combine(songsFolder, beatmapPath);
                if (File.Exists(candidate))
                {
                    return Success(Path.GetFullPath(candidate));
                }
            }
        }

        return Error<string>(
            "Beatmap file from fallback IPC does not exist. Check configured Songs folder in Settings.");
    }

    private IEnumerable<string> EnumerateSongsFolders()
    {
        var configuredOrDetected = SongsPathResolver.ResolveSongsPath(settingsService, songLibraryService);
        if (!string.IsNullOrWhiteSpace(configuredOrDetected))
        {
            yield return configuredOrDetected;
        }

        var running = OsuStableInstallLocator.TryGetRunningSongsFolder();
        if (!string.IsNullOrWhiteSpace(running) &&
            !string.Equals(running, configuredOrDetected, StringComparison.Ordinal))
        {
            yield return running;
        }
    }

    private static Result<string> Success(string value) => new()
    {
        Value = value,
        Status = ResultStatus.Success,
        ErrorMessage = null
    };

    private static Result<T> Error<T>(string message) => new()
    {
        Value = default,
        Status = ResultStatus.Error,
        ErrorMessage = message
    };
}
