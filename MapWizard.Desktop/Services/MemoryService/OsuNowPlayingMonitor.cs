using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Utils;

namespace MapWizard.Desktop.Services.MemoryService;

/// <summary>
/// Keeps track of the beatmap open in osu!, preferring an osu!lazer external-edit mount over the
/// beatmap selected in osu!stable. Polls in the background and publishes a new
/// <see cref="OsuNowPlaying"/> on the UI thread only when the open beatmap changes.
/// </summary>
public sealed partial class OsuNowPlayingMonitor(
    IOsuMemoryReaderService osuMemoryReaderService,
    ILazerLookupService lazerLookupService) : ObservableObject, IDisposable
{
    private static readonly TimeSpan ForegroundPollInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Nothing reads the now-playing card while MapWizard is in the background, so polling slows
    /// down there and catches up as soon as the window is focused again.
    /// </summary>
    private static readonly TimeSpan BackgroundPollInterval = TimeSpan.FromSeconds(45);
    private const int BackgroundDecodeWidth = 320;

    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);
    private volatile bool _isForeground = true;
    private CancellationTokenSource? _pollCts;
    private string? _currentKey;
    private string? _lastLoggedError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrent))]
    private OsuNowPlaying? _current;

    public bool HasCurrent => Current is not null;

    /// <summary>
    /// The beatmap selected in osu!stable, tracked separately because <see cref="Current"/> gives
    /// priority to osu!lazer.
    /// </summary>
    public string? StableBeatmapPath { get; private set; }

    public void Start()
    {
        if (_pollCts is not null)
        {
            return;
        }

        _pollCts = new CancellationTokenSource();
        _ = PollAsync(_pollCts.Token);
    }

    /// <summary>
    /// Switches between the foreground and background poll rates; regaining focus polls right away.
    /// </summary>
    public void SetForeground(bool isForeground)
    {
        _isForeground = isForeground;
        if (!isForeground)
        {
            return;
        }

        try
        {
            _wakeSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // A wake-up is already pending.
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            var probe = await Task.Run(Probe, cancellationToken);
            StableBeatmapPath = probe.StableBeatmapPath;
            if (probe.Key == _currentKey)
            {
                return;
            }

            _currentKey = probe.Key;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var previous = Current;
                Current = probe.Snapshot;
                previous?.Dispose();
            });
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
        Current?.Dispose();
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                try
                {
                    await RefreshAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogOnce(ex);
                }

                var interval = _isForeground ? ForegroundPollInterval : BackgroundPollInterval;
                await _wakeSignal.WaitAsync(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private ProbeResult Probe()
    {
        var stableResult = osuMemoryReaderService.GetBeatmapPath();
        var stablePath = stableResult.Status == ResultStatus.Success && !string.IsNullOrWhiteSpace(stableResult.Value)
            ? stableResult.Value
            : null;

        var lazerResult = lazerLookupService.GetMountedSessionState();
        if (lazerResult.Status == ResultStatus.Success &&
            lazerResult.Value?.MountedBeatmapPaths is { Count: > 0 } mountedPaths)
        {
            var key = BuildKey(OsuClient.Lazer, mountedPaths[0], Path.GetDirectoryName(mountedPaths[0]));
            return new ProbeResult(key, stablePath, key == _currentKey
                ? null
                : TryBuildSnapshot(OsuClient.Lazer, mountedPaths));
        }

        if (stablePath is not null)
        {
            var key = BuildKey(OsuClient.Stable, stablePath, stablePath);
            return new ProbeResult(key, stablePath, key == _currentKey
                ? null
                : TryBuildSnapshot(OsuClient.Stable, [stablePath]));
        }

        return new ProbeResult(null, null, null);
    }

    /// <summary>
    /// Identifies what is open, including the last write time so edits saved from MapWizard or the
    /// client (e.g. new metadata) refresh the snapshot.
    /// </summary>
    private static string BuildKey(OsuClient client, string path, string? timestampPath)
    {
        var lastWrite = DateTime.MinValue;
        try
        {
            if (timestampPath is not null)
            {
                lastWrite = File.GetLastWriteTimeUtc(timestampPath);
            }
        }
        catch (Exception)
        {
            // A missing timestamp only means a change may be picked up one poll later.
        }

        return $"{client}|{path}|{lastWrite.Ticks}";
    }

    private OsuNowPlaying? TryBuildSnapshot(OsuClient client, IReadOnlyList<string> beatmapPaths)
    {
        try
        {
            var primaryPath = beatmapPaths[0];
            var metadata = BeatmapCardInfoReader.Read(primaryPath);
            var artist = StringValueUtils.FirstNonEmpty(metadata.Artist, metadata.ArtistUnicode, "Unknown Artist");
            var title = StringValueUtils.FirstNonEmpty(metadata.Title, metadata.TitleUnicode, "Unknown Title");
            var creator = StringValueUtils.FirstNonEmpty(metadata.Creator, "Unknown Mapper");

            // Lazer mounts the whole set and does not say which difficulty is open.
            var difficulty = client == OsuClient.Lazer && beatmapPaths.Count > 1
                ? null
                : StringValueUtils.FirstNonEmpty(metadata.Version, "Unknown Difficulty");

            return new OsuNowPlaying(
                client,
                Path.GetDirectoryName(primaryPath) ?? string.Empty,
                beatmapPaths,
                artist,
                title,
                difficulty,
                creator,
                TryDecodeBackground(primaryPath, metadata.BackgroundFilename));
        }
        catch (Exception ex)
        {
            LogOnce(ex);
            return null;
        }
    }

    private static Bitmap? TryDecodeBackground(string beatmapPath, string? backgroundFilename)
    {
        try
        {
            var backgroundPath = MapsetAssetPathUtils.ResolveRelativePathFromBeatmap(beatmapPath, backgroundFilename);
            return backgroundPath is not null && File.Exists(backgroundPath)
                ? ArtworkBitmapUtils.DecodePreview(backgroundPath, BackgroundDecodeWidth)
                : null;
        }
        catch (Exception)
        {
            // Unsupported or corrupt artwork falls back to the placeholder.
            return null;
        }
    }

    /// <summary>
    /// Polling repeats every couple of seconds, so a persistent failure is only logged once.
    /// </summary>
    private void LogOnce(Exception ex)
    {
        if (ex.Message == _lastLoggedError)
        {
            return;
        }

        _lastLoggedError = ex.Message;
        MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
    }

    private sealed record ProbeResult(string? Key, string? StableBeatmapPath, OsuNowPlaying? Snapshot);
}
