using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MapWizard.Desktop.Models.Settings;
using MiniAudioEx.Core.StandardAPI;

namespace MapWizard.Desktop.Services.Playback;

public sealed class MiniAudioPlaybackService : IAudioPlaybackService, IDisposable
{
    private const uint SampleRate = 44100;
    private const uint Channels = 2;
    // Smaller period lowers output latency for editor-preview style transport.
    private const uint PeriodSizeInFrames = 256;
    // Drive MiniAudioEx updates off the UI thread at a fixed high rate for stable transport timing.
    private const int AudioUpdateRateHz = 1000;
    private const string AudioUpdateThreadName = "MapWizard.AudioUpdate";
    private static readonly TimeSpan AudioUpdateShutdownWait = TimeSpan.FromMilliseconds(500);
    private const int HitsoundSourceMaxVoices = 256;

    private readonly object _sync = new();
    private readonly Dictionary<string, AudioClip> _clipCache = new(StringComparer.OrdinalIgnoreCase);

    private bool _initialized;
    private CancellationTokenSource? _updateLoopCts;
    private Thread? _updateLoopThread;
    private AudioSource? _songSource;
    private readonly Dictionary<int, AudioSource> _hitsoundSourcesByVolumePercent = [];
    private float _hitsoundVolume = 1f;
    private AudioClip? _songClip;
    private string _songPath = string.Empty;
    private ulong _songSourceLengthFrames;
    private int _songDurationMs;
    private int _lastSeekRequestMs = -1;
    private int _lastSeekObservedMs = -1;
    private int _lastSeekErrorMs;
    private long _audioUpdateLastTickMs;
    private int _audioUpdateLastIntervalMs;
    private int _audioUpdateMaxIntervalMs;
    private int _audioUpdateLastLoopMs;
    private int _audioUpdateMaxLoopMs;

    public bool IsSongPlaying
    {
        get
        {
            lock (_sync)
            {
                return _songSource?.IsPlaying == true;
            }
        }
    }

    public bool LoadSong(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            lock (_sync)
            {
                ClearLoadedSongState();
            }

            return false;
        }

        lock (_sync)
        {
            EnsureInitialized();

            if (string.Equals(_songPath, filePath, StringComparison.OrdinalIgnoreCase) && _songClip is not null)
            {
                return true;
            }

            _songClip = GetOrCreateClip(filePath, streamFromDisk: true);
            _songPath = filePath;
            ResetLoadedSongRuntimeState();
            if (_songClip is null)
            {
                ResetLoadedSongRuntimeState();
            }

            return _songClip is not null;
        }
    }

    public bool PlaySong(int startTimeMs)
    {
        lock (_sync)
        {
            if (_songClip is null)
            {
                return false;
            }

            EnsureInitialized();
            if (_songSource is null)
            {
                return false;
            }

            _songSource.Stop();
            _songSource.Cursor = 0;
            _songSource.Play(_songClip);
            AudioContext.Update();
            var targetFrame = MillisecondsToFrames(startTimeMs, GetAudioFrameRateHz());
            SetSongCursor(_songSource, targetFrame);

            // Streaming clips may not expose a seekable length/cursor immediately on the same tick.
            // Retry once after another update so transport seeks do not restart from 0.
            if (startTimeMs > 0 && _songSource.Cursor == 0)
            {
                AudioContext.Update();
                SetSongCursor(_songSource, targetFrame);
            }

            UpdateLastSeekDebug(_songSource, startTimeMs);

            return true;
        }
    }

    public void PauseSong()
    {
        lock (_sync)
        {
            _songSource?.Stop();
        }
    }

    public void StopSong()
    {
        lock (_sync)
        {
            if (_songSource is null)
            {
                return;
            }

            _songSource.Stop();
            _songSource.Cursor = 0;
        }
    }

    public int GetSongPositionMs()
    {
        lock (_sync)
        {
            if (_songSource is null)
            {
                return 0;
            }

            return GetObservedSongPositionMs(_songSource);
        }
    }

    public int GetLoadedSongDurationMs()
    {
        lock (_sync)
        {
            if (_songClip is null)
            {
                return 0;
            }

            try
            {
                EnsureInitialized();
                if (_songSource is null)
                {
                    return Math.Max(0, _songDurationMs);
                }

                ResetSongSourcePlaybackState(_songSource);
                _songSource.Play(_songClip);

                var lengthFrames = TryReadSongSourceLengthFrames(_songSource, attempts: 8);

                if (lengthFrames > 0)
                {
                    _songSourceLengthFrames = lengthFrames;
                    var durationMs = FramesToMilliseconds(lengthFrames, GetAudioFrameRateHz());
                    _songDurationMs = durationMs;
                    ResetSongSourcePlaybackState(_songSource);
                    return durationMs;
                }

                ResetSongSourcePlaybackState(_songSource);

                // Fallback when source length is unavailable (can happen on some files/code paths).
                _songDurationMs = 0;
                _songSourceLengthFrames = 0;
                return _songDurationMs;
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                return 0;
            }
        }
    }

    public string GetTimingTelemetryStatus()
    {
        var isInitialized = _initialized;
        var isThreadAlive = _updateLoopThread?.IsAlive == true;
        var lastIntervalMs = Volatile.Read(ref _audioUpdateLastIntervalMs);
        var maxIntervalMs = Volatile.Read(ref _audioUpdateMaxIntervalMs);
        var lastLoopMs = Volatile.Read(ref _audioUpdateLastLoopMs);
        var maxLoopMs = Volatile.Read(ref _audioUpdateMaxLoopMs);

        if (!isInitialized)
        {
            return $"Audio[{AudioUpdateThreadName}]: idle";
        }

        var threadState = isThreadAlive ? "alive" : "stopped";
        return $"Audio[{AudioUpdateThreadName}/{AudioUpdateRateHz}Hz/{threadState}] dt {lastIntervalMs}ms (max {maxIntervalMs}ms) loop {lastLoopMs}ms (max {maxLoopMs}ms)";
    }

    public string GetSongDebugStatus()
    {
        lock (_sync)
        {
            if (_songClip is null)
            {
                return "AudioDbg: no song";
            }

            var outputRateHz = _initialized ? Math.Max(1, AudioContext.SampleRate) : (int)SampleRate;
            var sourceLengthFrames = _songSourceLengthFrames;
            if (sourceLengthFrames == 0 && _songSource is not null)
            {
                sourceLengthFrames = SafeReadSourceLength(_songSource);
            }

            var cursorFrames = _songSource is not null ? SafeReadSourceCursor(_songSource) : 0UL;

            var rateHz = GetAudioFrameRateHz();
            var cursorMs = FramesToMilliseconds(cursorFrames, rateHz);
            var sourceDurationMs = sourceLengthFrames > 0 ? FramesToMilliseconds(sourceLengthFrames, rateHz) : 0;
            var ext = Path.GetExtension(_songPath);
            var seekState = _lastSeekRequestMs < 0
                ? "seek:none"
                : $"seek:req {_lastSeekRequestMs} obs {_lastSeekObservedMs} err {_lastSeekErrorMs}ms";

            return $"AudioDbg | ext {ext} out {outputRateHz}Hz srcLenF {sourceLengthFrames} srcMs {sourceDurationMs} curF {cursorFrames} curMs {cursorMs} {seekState}";
        }
    }

    public void SetSongVolume(float volume)
    {
        lock (_sync)
        {
            EnsureInitialized();
            if (_songSource is not null)
            {
                _songSource.Volume = Clamp01(volume);
            }
        }
    }

    public void SetHitsoundVolume(float volume)
    {
        lock (_sync)
        {
            EnsureInitialized();

            _hitsoundVolume = Clamp01(volume);
            foreach (var entry in _hitsoundSourcesByVolumePercent)
            {
                entry.Value.Volume = _hitsoundVolume * (entry.Key / 100f);
            }
        }
    }

    public bool PlayHitsound(string filePath, float volumeMultiplier = 1f, string playbackBusKey = "")
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        lock (_sync)
        {
            EnsureInitialized();

            var effectiveVolumePercent = (int)Math.Round(Clamp01(volumeMultiplier) * 100f);
            var source = GetOrCreateHitsoundSource(effectiveVolumePercent);
            if (source is null)
            {
                return false;
            }

            var clip = GetOrCreateClip(filePath, streamFromDisk: false);
            if (clip is null)
            {
                return false;
            }

            source.PlayOneShot(clip);
            return true;
        }
    }

    public IReadOnlyList<AudioOutputDeviceOption> GetAudioOutputDevices()
    {
        return
        [
            new AudioOutputDeviceOption("default", "System Default (MiniAudio)", isDefault: true, isEnabled: true)
        ];
    }

    public string GetSelectedAudioOutputDeviceId()
    {
        return "default";
    }

    public bool SetSelectedAudioOutputDevice(string deviceId)
    {
        // MiniAudio implementation currently uses the default output device only.
        return true;
    }

    public void Dispose()
    {
        CancellationTokenSource? updateLoopCts;
        Thread? updateLoopThread;

        lock (_sync)
        {
            updateLoopCts = _updateLoopCts;
            updateLoopThread = _updateLoopThread;
            _updateLoopCts = null;
            _updateLoopThread = null;
        }

        if (updateLoopCts is not null)
        {
            try
            {
                updateLoopCts.Cancel();
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                // Ignore shutdown cancellation failures.
            }

            try
            {
                updateLoopThread?.Join(AudioUpdateShutdownWait);
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                // Ignore worker shutdown failures during app shutdown/disposal.
            }
            finally
            {
                updateLoopCts.Dispose();
            }
        }

        lock (_sync)
        {
            _songSource?.Dispose();
            _songSource = null;

            foreach (var source in _hitsoundSourcesByVolumePercent.Values)
            {
                try
                {
                    source.Dispose();
                }
                catch (Exception ex)
                {
                    MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                    // Ignore hitsound source disposal failures on shutdown.
                }
            }

            _hitsoundSourcesByVolumePercent.Clear();

            foreach (var clip in _clipCache.Values)
            {
                try
                {
                    clip.Dispose();
                }
                catch (Exception ex)
                {
                    MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                    // Ignore clip disposal failures on shutdown.
                }
            }

            _clipCache.Clear();
            ClearLoadedSongState();

            if (_initialized)
            {
                try
                {
                    AudioContext.Deinitialize();
                }
                catch (Exception ex)
                {
                    MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                    // Ignore deinit failures during app shutdown.
                }
                finally
                {
                    _initialized = false;
                }
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        AudioContext.Initialize(SampleRate, Channels, PeriodSizeInFrames, deviceInfo: null!);
        _songSource = new AudioSource(maxSources: 1);
        _hitsoundSourcesByVolumePercent.Clear();
        _hitsoundSourcesByVolumePercent[100] = new AudioSource(maxSources: HitsoundSourceMaxVoices)
        {
            Volume = _hitsoundVolume
        };
        _initialized = true;
        ResetAudioUpdateTelemetry();
        _updateLoopCts = new CancellationTokenSource();
        _updateLoopThread = new Thread(() => RunAudioUpdateLoop(_updateLoopCts.Token))
        {
            IsBackground = true,
            Name = AudioUpdateThreadName
        };
        _updateLoopThread.Start();
    }

    private void RunAudioUpdateLoop(CancellationToken token)
    {
        var targetStepTicks = Math.Max(1L, Stopwatch.Frequency / Math.Max(1, AudioUpdateRateHz));
        var clock = Stopwatch.StartNew();
        var nextTick = clock.ElapsedTicks;

        while (!token.IsCancellationRequested)
        {
            var loopStartTickMs = Environment.TickCount64;
            var previousTickMs = Interlocked.Exchange(ref _audioUpdateLastTickMs, loopStartTickMs);
            if (previousTickMs > 0)
            {
                var intervalMs = (int)Math.Clamp(loopStartTickMs - previousTickMs, 0, int.MaxValue);
                Volatile.Write(ref _audioUpdateLastIntervalMs, intervalMs);
                UpdateMax(ref _audioUpdateMaxIntervalMs, intervalMs);
            }

            try
            {
                lock (_sync)
                {
                    if (!_initialized)
                    {
                        return;
                    }

                    AudioContext.Update();
                }
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                // Keep the audio update loop resilient; a transient update failure should not crash the app.
            }

            nextTick += targetStepTicks;

            while (!token.IsCancellationRequested)
            {
                var remainingTicks = nextTick - clock.ElapsedTicks;
                if (remainingTicks <= 0)
                {
                    break;
                }

                var remainingMs = (remainingTicks * 1000d) / Stopwatch.Frequency;
                if (remainingMs >= 2d)
                {
                    Thread.Sleep(Math.Max(0, (int)remainingMs - 1));
                }
                else
                {
                    Thread.Yield();
                }
            }

            // If the app was stalled, resync instead of trying to "catch up" thousands of iterations.
            var driftTicks = clock.ElapsedTicks - nextTick;
            if (driftTicks > targetStepTicks * 8)
            {
                nextTick = clock.ElapsedTicks;
            }

            var loopDurationMs = (int)Math.Clamp(Environment.TickCount64 - loopStartTickMs, 0, int.MaxValue);
            Volatile.Write(ref _audioUpdateLastLoopMs, loopDurationMs);
            UpdateMax(ref _audioUpdateMaxLoopMs, loopDurationMs);
        }
    }

    private void ResetAudioUpdateTelemetry()
    {
        Interlocked.Exchange(ref _audioUpdateLastTickMs, 0);
        Volatile.Write(ref _audioUpdateLastIntervalMs, 0);
        Volatile.Write(ref _audioUpdateMaxIntervalMs, 0);
        Volatile.Write(ref _audioUpdateLastLoopMs, 0);
        Volatile.Write(ref _audioUpdateMaxLoopMs, 0);
    }

    private static void UpdateMax(ref int target, int candidate)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (candidate <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }

    private AudioClip? GetOrCreateClip(string filePath, bool streamFromDisk)
    {
        var fullPath = Path.GetFullPath(filePath);
        var cacheKey = $"{(streamFromDisk ? "stream" : "mem")}::{Path.GetFullPath(filePath)}";
        if (_clipCache.TryGetValue(cacheKey, out var existing))
        {
            return existing;
        }

        try
        {
            var clip = new AudioClip(fullPath, streamFromDisk);
            _clipCache[cacheKey] = clip;
            return clip;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
    }

    private static void SetSongCursor(AudioSource source, ulong targetFrame)
    {
        try
        {
            var length = source.Length;
            if (length > 1)
            {
                source.Cursor = Math.Min(targetFrame, length - 2);
                return;
            }

            if (length == 1)
            {
                source.Cursor = 0;
                return;
            }
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            // Fall through and try direct cursor set when length isn't available yet.
        }

        try
        {
            source.Cursor = targetFrame;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            // Ignore seek failures; caller will continue from current playback position.
        }
    }

    private static ulong MillisecondsToFrames(int timeMs, int sampleRate)
    {
        var clampedMs = Math.Max(0, timeMs);
        var frames = (clampedMs / 1000d) * Math.Max(1, sampleRate);
        return (ulong)Math.Max(0, Math.Round(frames));
    }

    private static int FramesToMilliseconds(ulong frames, int sampleRate)
    {
        var sr = Math.Max(1, sampleRate);
        var ms = (frames * 1000d) / sr;
        return (int)Math.Max(0, Math.Round(ms));
    }

    private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    private AudioSource? GetOrCreateHitsoundSource(int volumePercent)
    {
        var clampedPercent = Math.Clamp(volumePercent, 0, 100);
        if (_hitsoundSourcesByVolumePercent.TryGetValue(clampedPercent, out var source))
        {
            return source;
        }

        try
        {
            source = new AudioSource(maxSources: HitsoundSourceMaxVoices)
            {
                Volume = _hitsoundVolume * (clampedPercent / 100f)
            };
            _hitsoundSourcesByVolumePercent[clampedPercent] = source;
            return source;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
    }

    private void ClearLoadedSongState()
    {
        _songClip = null;
        _songPath = string.Empty;
        ResetLoadedSongRuntimeState();
    }

    private void ResetLoadedSongRuntimeState()
    {
        _songSourceLengthFrames = 0;
        _songDurationMs = 0;
        ResetSongSeekDebugState();
    }

    private static void ResetSongSourcePlaybackState(AudioSource source)
    {
        source.Stop();
        source.Cursor = 0;
    }

    private ulong TryReadSongSourceLengthFrames(AudioSource source, int attempts)
    {
        ulong lengthFrames = 0;
        for (var i = 0; i < Math.Max(1, attempts); i++)
        {
            AudioContext.Update();
            lengthFrames = SafeReadSourceLength(source);
            if (lengthFrames > 0)
            {
                return lengthFrames;
            }
        }

        return 0;
    }

    private int GetObservedSongPositionMs(AudioSource source)
    {
        return FramesToMilliseconds(SafeReadSourceCursor(source), GetAudioFrameRateHz());
    }

    private int GetAudioFrameRateHz()
    {
        return _initialized ? Math.Max(1, AudioContext.SampleRate) : (int)SampleRate;
    }

    private static ulong SafeReadSourceLength(AudioSource source)
    {
        try
        {
            return source.Length;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return 0;
        }
    }

    private static ulong SafeReadSourceCursor(AudioSource source)
    {
        try
        {
            return source.Cursor;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return 0;
        }
    }

    private void UpdateLastSeekDebug(AudioSource source, int targetTimeMs)
    {
        _lastSeekRequestMs = Math.Max(0, targetTimeMs);

        try
        {
            AudioContext.Update();
            var observedMs = GetObservedSongPositionMs(source);
            _lastSeekObservedMs = observedMs;
            _lastSeekErrorMs = observedMs - _lastSeekRequestMs;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            _lastSeekObservedMs = -1;
            _lastSeekErrorMs = 0;
        }
    }

    private void ResetSongSeekDebugState()
    {
        _lastSeekRequestMs = -1;
        _lastSeekObservedMs = -1;
        _lastSeekErrorMs = 0;
    }

}
