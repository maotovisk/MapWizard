using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Utils;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using OsuWineMemReader;

namespace MapWizard.Desktop.Services;

public class OsuMemoryReaderService(ISettingsService settingsService, ISongLibraryService songLibraryService)
    : IOsuMemoryReaderService
{
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public Result<string> GetBeatmapPath()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows())
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "This feature is not yet supported on your operating system."
            };
        }

        return OperatingSystem.IsWindows() ? GetBeatmapWindows() : GetBeatmapLinux();
    }

    public Result<int> GetCurrentTimestamp()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            return new Result<int>
            {
                Value = 0,
                Status = ResultStatus.Error,
                ErrorMessage = "Timestamp reading is currently supported only on Windows and Linux."
            };
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

        return new Result<int>
        {
            Value = 0,
            Status = ResultStatus.Error,
            ErrorMessage = $"Unable to read current timestamp from fallback IPC. {ipcError}"
        };
    }

    [SupportedOSPlatform("linux")]
    private static Result<string> GetBeatmapLinux()
    {
        var memoryResult = GetBeatmapLinuxFromMemory();
        if (memoryResult.Status == ResultStatus.Success && !string.IsNullOrWhiteSpace(memoryResult.Value))
        {
            return memoryResult;
        }
        
        var fallbackIpcResult = GetBeatmapLinuxFromFallbackIpc();
        if (fallbackIpcResult.Status == ResultStatus.Success && !string.IsNullOrWhiteSpace(fallbackIpcResult.Value))
        {
            return fallbackIpcResult;
        }

        return new Result<string>
        {
            Value = null,
            Status = ResultStatus.Error,
            ErrorMessage = $"Memory read failed and fallback IPC path could not be resolved."
        };
    }

    private static Result<string> GetBeatmapLinuxFromMemory()
    {
        var options = new OsuMemory.OsuMemoryOptions()
        {
            WriteToFile = false,
            RunOnce = true
        };

        var running = true;
        try
        {
            OsuMemory.StartBeatmapPathReading(ref running, out var beatmapPath, options);

            return new Result<string>()
            {
                Value = beatmapPath,
                Status = ResultStatus.Success,
                ErrorMessage = null
            };
        }
        catch (Exception exception)
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = exception.Message
            };
        }
    }
    
    [SupportedOSPlatform("linux")]
    private static Result<string> GetBeatmapLinuxFromFallbackIpc()
    {
        if (!FallbackClientIpc.TryReadBeatmapPath(out var beatmapPath, out var ipcError))
        {
            return new Result<string>
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = ipcError ?? "Unable to read beatmap path from fallback IPC."
            };
        }

        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return new Result<string>
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Fallback IPC returned an empty beatmap path."
            };
        }

        if (!File.Exists(beatmapPath))
        {
            return new Result<string>
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Beatmap file from fallback IPC does not exist."
            };
        }

        return new Result<string>
        {
            Value = Path.GetFullPath(beatmapPath),
            Status = ResultStatus.Success,
            ErrorMessage = null
        };
    }

    [SupportedOSPlatform("windows")]
    private Result<string> GetBeatmapWindows()
    {
        var memoryResult = GetBeatmapWindowsFromMemory();
        if (memoryResult.Status == ResultStatus.Success && !string.IsNullOrWhiteSpace(memoryResult.Value))
        {
            return memoryResult;
        }

        var fallbackIpcResult = GetBeatmapWindowsFromFallbackIpc();
        if (fallbackIpcResult.Status == ResultStatus.Success && !string.IsNullOrWhiteSpace(fallbackIpcResult.Value))
        {
            return fallbackIpcResult;
        }

        return new Result<string>
        {
            Value = null,
            Status = ResultStatus.Error,
            ErrorMessage = $"Memory read failed and fallback IPC path could not be resolved."
        };
    }
    
    [SupportedOSPlatform("windows")]
    private Result<string> GetBeatmapWindowsFromMemory()
    {
        var reader = StructuredOsuMemoryReader.GetInstance(null);

        if (reader == null)
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Unable to create reader instance."
            };
        }

        var currentBeatmap = new CurrentBeatmap();
        if (!reader.TryRead(currentBeatmap))
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Unable to read current beatmap."
            };
        }

        if (string.IsNullOrEmpty(currentBeatmap.OsuFileName))
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "No beatmap is currently loaded."
            };
        }

        var songsFolder = GetSongsFolderWindows();
        if (string.IsNullOrWhiteSpace(songsFolder))
        {
            return new Result<string>
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Unable to resolve Songs folder."
            };
        }

        var beatmapPath = Path.Combine(songsFolder, currentBeatmap.FolderName, currentBeatmap.OsuFileName);

        if (!File.Exists(beatmapPath))
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Beatmap file does not exist."
            };
        }

        return new Result<string>()
        {
            Value = beatmapPath,
            Status = ResultStatus.Success,
            ErrorMessage = null
        };
    }

    [SupportedOSPlatform("windows")]
    private Result<string> GetBeatmapWindowsFromFallbackIpc()
    {
        if (!FallbackClientIpc.TryReadBeatmapPath(out var beatmapPath, out var ipcError))
        {
            return new Result<string>
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = ipcError ?? "Unable to read beatmap path from fallback IPC."
            };
        }

        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return new Result<string>
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Fallback IPC returned an empty beatmap path."
            };
        }

        beatmapPath = beatmapPath.Trim().Trim('"');
        var songsFolder = GetSongsFolderWindows();

        var candidates = new[]
        {
            beatmapPath,
            !Path.IsPathRooted(beatmapPath) && !string.IsNullOrWhiteSpace(songsFolder)
                ? Path.Combine(songsFolder, beatmapPath)
                : string.Empty
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var fullCandidate = Path.GetFullPath(candidate);
            if (File.Exists(fullCandidate))
            {
                return new Result<string>
                {
                    Value = fullCandidate,
                    Status = ResultStatus.Success,
                    ErrorMessage = null
                };
            }
        }

        if (!File.Exists(beatmapPath))
        {
            return new Result<string>
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "Beatmap file from fallback IPC does not exist. Check configured Songs folder in Settings."
            };
        }

        return new Result<string>
        {
            Value = Path.GetFullPath(beatmapPath),
            Status = ResultStatus.Success,
            ErrorMessage = null
        };
    }

    [SupportedOSPlatform("windows")]
    private string GetSongsFolderWindows()
    {
        var configuredOrDetected = SongsPathResolver.ResolveSongsPath(settingsService, songLibraryService);
        if (!string.IsNullOrWhiteSpace(configuredOrDetected))
        {
            return configuredOrDetected;
        }

        return GetRunningSongsFolderWindows();
    }

    [SupportedOSPlatform("windows")]
    private static string GetRunningSongsFolderWindows()
    {
        var processes = Process.GetProcessesByName("osu!");
        if (processes.Length <= 0) return string.Empty;
        
        var path = processes[0].Modules[0].FileName;
        
        var basePath = path.Remove(path.LastIndexOf('\\'));
        
        var configPath = $"osu!.{Environment.UserName}.cfg";
        var configFile = File.OpenRead(Path.Combine(basePath, configPath));
        using var reader = new StreamReader(configFile);
        while (reader.ReadLine() is { } line)
        {
            if (!line.StartsWith("BeatmapDirectory")) continue;
            
            var customPath = line.Split('=')[1].Trim();
            return Path.IsPathRooted(customPath) ? customPath : Path.Combine(basePath, customPath);
        }
        
        if (!string.IsNullOrEmpty(path))
        {
            path = Path.Combine(path, "Songs");
        }

        return path;
    }
}
