using System;
using System.Diagnostics;
using System.IO;
using MapWizard.Desktop.Models;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using OsuWineMemReader;

namespace MapWizard.Desktop.Services;

public class OsuMemoryReaderService : IOsuMemoryReaderService
{
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

    private static Result<string> GetBeatmapLinux()
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

    private static Result<string> GetBeatmapWindows()
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

        var songsFolder = GetRunningSongsFolderWindows();

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