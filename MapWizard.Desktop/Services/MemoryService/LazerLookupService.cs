using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services.MemoryService;

/// <summary>
/// Finds beatmaps exposed by osu!lazer's "Edit externally" operation.
/// </summary>
/// <remarks>
/// lazer mounts a beatmap set at <c>Path.GetTempPath()/&lt;beatmap-set SHA-256&gt;</c> and removes
/// it after the user finishes external editing. Reading and writing that mount lets lazer remain
/// the sole owner of its Realm database and content-addressed file store.
/// </remarks>
public sealed class LazerLookupService : ILazerLookupService
{
    private static readonly string[] lazerProcessNames = ["osu!", "osu", "osu.Desktop"];

    public Result<IReadOnlyList<string>> GetMountedBeatmapPaths()
    {
        try
        {
            var processStartUtc = GetRunningLazerStartTimeUtc();
            if (processStartUtc is null)
            {
                return Success([]);
            }

            var mountedSet = Directory.EnumerateDirectories(Path.GetTempPath(), "*", SearchOption.TopDirectoryOnly)
                .Where(path => IsSha256DirectoryName(Path.GetFileName(path)))
                .Select(path => TryReadMountedSet(path, processStartUtc.Value))
                .Where(candidate => candidate is not null)
                .OrderByDescending(candidate => candidate!.LastWriteUtc)
                .FirstOrDefault();

            return Success(mountedSet?.BeatmapPaths ?? []);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return new Result<IReadOnlyList<string>>
            {
                Value = [],
                Status = ResultStatus.Error,
                ErrorMessage = $"Unable to inspect osu!lazer's external-edit folder. {ex.Message}"
            };
        }
    }

    private static DateTime? GetRunningLazerStartTimeUtc()
    {
        DateTime? latestStartUtc = null;

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (!IsLazerProcess(process))
                    {
                        continue;
                    }

                    var startUtc = process.StartTime.ToUniversalTime();
                    if (latestStartUtc is null || startUtc > latestStartUtc)
                    {
                        latestStartUtc = startUtc;
                    }
                }
                catch
                {
                    // Processes may exit or deny metadata access while they are being enumerated.
                }
            }
        }

        return latestStartUtc;
    }

    private static bool IsLazerProcess(Process process)
    {
        if (!lazerProcessNames.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var productName = process.MainModule?.FileVersionInfo.ProductName;
            if (!string.IsNullOrWhiteSpace(productName))
            {
                return productName.Contains("lazer", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Process name plus a valid external-edit mount is the cross-platform fallback.
        }

        return true;
    }

    private static MountedSet? TryReadMountedSet(string directoryPath, DateTime processStartUtc)
    {
        try
        {
            var lastWriteUtc = Directory.GetLastWriteTimeUtc(directoryPath);

            // A previous crash can leave a mount behind. Only accept mounts created by the
            // currently running client (with a small allowance for filesystem timestamp precision).
            if (lastWriteUtc < processStartUtc.AddSeconds(-5))
            {
                return null;
            }

            var beatmapPaths = Directory.EnumerateFiles(directoryPath, "*.osu", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFullPath)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return beatmapPaths.Length == 0 ? null : new MountedSet(lastWriteUtc, beatmapPaths);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool IsSha256DirectoryName(string name)
    {
        if (name.Length != 64)
        {
            return false;
        }

        foreach (var character in name)
        {
            if (!char.IsAsciiHexDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static Result<IReadOnlyList<string>> Success(IReadOnlyList<string> paths) => new()
    {
        Value = paths,
        Status = ResultStatus.Success
    };

    private sealed record MountedSet(DateTime LastWriteUtc, IReadOnlyList<string> BeatmapPaths);
}
