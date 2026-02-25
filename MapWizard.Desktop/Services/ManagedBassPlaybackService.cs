using System;
using System.Collections.Generic;
using System.IO;
using ManagedBass;

namespace MapWizard.Desktop.Services;

public sealed class ManagedBassPlaybackService : IAudioPlaybackService, IDisposable
{
    private const int OutputSampleRateHz = 44100;
    private const int PlaybackBufferLengthMs = 25;
    private const int UpdatePeriodMs = 5;
    private const int HitsoundSampleMaxVoices = 64;

    private readonly object _sync = new();
    private readonly Dictionary<string, int> _hitsoundSampleCache = new(StringComparer.OrdinalIgnoreCase);

    private bool _initialized;
    private int _songStreamHandle;
    private string _songPath = string.Empty;
    private long _songLengthBytes;
    private int _songDurationMs;
    private float _songVolume = 1f;
    private float _hitsoundVolume = 1f;
    private int _lastSeekRequestMs = -1;
    private int _lastSeekObservedMs = -1;
    private int _lastSeekErrorMs;
    private Errors _lastBassError = Errors.OK;

    public bool IsSongPlaying
    {
        get
        {
            lock (_sync)
            {
                return _songStreamHandle != 0 && Bass.ChannelIsActive(_songStreamHandle) == PlaybackState.Playing;
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
            if (!EnsureInitialized())
            {
                ClearLoadedSongState();
                return false;
            }

            var fullPath = Path.GetFullPath(filePath);
            if (_songStreamHandle != 0 && string.Equals(_songPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            FreeSongStream();

            var streamHandle = Bass.CreateStream(fullPath, 0, 0, BassFlags.Prescan);
            if (streamHandle == 0)
            {
                _lastBassError = Bass.LastError;
                ClearLoadedSongState();
                return false;
            }

            _songStreamHandle = streamHandle;
            _songPath = fullPath;
            ResetLoadedSongRuntimeState();
            ApplySongVolume();
            CacheSongLength();
            return true;
        }
    }

    public bool PlaySong(int startTimeMs)
    {
        lock (_sync)
        {
            if (_songStreamHandle == 0 || !EnsureInitialized())
            {
                return false;
            }

            _ = Bass.ChannelStop(_songStreamHandle);

            var targetMs = Math.Max(0, startTimeMs);
            var targetPositionBytes = MillisecondsToStreamBytes(_songStreamHandle, targetMs);
            if (_songLengthBytes > 0)
            {
                targetPositionBytes = Math.Clamp(targetPositionBytes, 0L, Math.Max(0L, _songLengthBytes - 1));
            }

            _ = Bass.ChannelSetPosition(_songStreamHandle, targetPositionBytes, PositionFlags.Bytes);
            if (!Bass.ChannelPlay(_songStreamHandle, false))
            {
                _lastBassError = Bass.LastError;
                UpdateLastSeekDebug(targetMs);
                return false;
            }

            UpdateLastSeekDebug(targetMs);
            return true;
        }
    }

    public void PauseSong()
    {
        lock (_sync)
        {
            if (_songStreamHandle != 0)
            {
                _ = Bass.ChannelPause(_songStreamHandle);
            }
        }
    }

    public void StopSong()
    {
        lock (_sync)
        {
            if (_songStreamHandle == 0)
            {
                return;
            }

            _ = Bass.ChannelStop(_songStreamHandle);
            _ = Bass.ChannelSetPosition(_songStreamHandle, 0, PositionFlags.Bytes);
        }
    }

    public int GetSongPositionMs()
    {
        lock (_sync)
        {
            if (_songStreamHandle == 0)
            {
                return 0;
            }

            return GetObservedSongPositionMs(_songStreamHandle);
        }
    }

    public int GetLoadedSongDurationMs()
    {
        lock (_sync)
        {
            if (_songStreamHandle == 0)
            {
                return 0;
            }

            if (_songDurationMs > 0)
            {
                return _songDurationMs;
            }

            CacheSongLength();
            return _songDurationMs;
        }
    }

    public string GetTimingTelemetryStatus()
    {
        lock (_sync)
        {
            if (!_initialized)
            {
                return "Audio[ManagedBass]: idle";
            }

            return $"Audio[ManagedBass/event-driven] upd {Bass.UpdatePeriod}ms buf {Bass.PlaybackBufferLength}ms err {_lastBassError}";
        }
    }

    public string GetSongDebugStatus()
    {
        lock (_sync)
        {
            if (_songStreamHandle == 0)
            {
                return $"AudioDbg: no song (ManagedBass err {_lastBassError})";
            }

            var ext = Path.GetExtension(_songPath);
            var lengthBytes = _songLengthBytes > 0 ? _songLengthBytes : SafeChannelGetLengthBytes(_songStreamHandle);
            var lengthMs = lengthBytes > 0 ? StreamBytesToMilliseconds(_songStreamHandle, lengthBytes) : 0;
            var cursorBytes = SafeChannelGetPositionBytes(_songStreamHandle);
            var cursorMs = StreamBytesToMilliseconds(_songStreamHandle, cursorBytes);
            var state = Bass.ChannelIsActive(_songStreamHandle);
            var seekState = _lastSeekRequestMs < 0
                ? "seek:none"
                : $"seek:req {_lastSeekRequestMs} obs {_lastSeekObservedMs} err {_lastSeekErrorMs}ms";

            return $"AudioDbg | backend managedbass ext {ext} state {state} lenB {lengthBytes} lenMs {lengthMs} curB {cursorBytes} curMs {cursorMs} {seekState} err {_lastBassError}";
        }
    }

    public void SetSongVolume(float volume)
    {
        lock (_sync)
        {
            _songVolume = Clamp01(volume);
            ApplySongVolume();
        }
    }

    public void SetHitsoundVolume(float volume)
    {
        lock (_sync)
        {
            _hitsoundVolume = Clamp01(volume);
        }
    }

    public bool PlayHitsound(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        lock (_sync)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            var sampleHandle = GetOrLoadHitsoundSample(filePath);
            if (sampleHandle == 0)
            {
                return false;
            }

            var channelHandle = Bass.SampleGetChannel(sampleHandle, false);
            if (channelHandle == 0)
            {
                _lastBassError = Bass.LastError;
                return false;
            }

            _ = Bass.ChannelSetAttribute(channelHandle, ChannelAttribute.Volume, _hitsoundVolume);
            if (!Bass.ChannelPlay(channelHandle, false))
            {
                _lastBassError = Bass.LastError;
                return false;
            }

            return true;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            FreeSongStream();

            foreach (var sampleHandle in _hitsoundSampleCache.Values)
            {
                try
                {
                    if (sampleHandle != 0)
                    {
                        _ = Bass.SampleFree(sampleHandle);
                    }
                }
                catch
                {
                    // Ignore sample disposal failures during app shutdown.
                }
            }

            _hitsoundSampleCache.Clear();
            ClearLoadedSongState();

            if (_initialized)
            {
                try
                {
                    _ = Bass.Free();
                }
                catch
                {
                    // Ignore backend free failures during shutdown.
                }
                finally
                {
                    _initialized = false;
                }
            }
        }
    }

    private bool EnsureInitialized()
    {
        if (_initialized)
        {
            return true;
        }

        try
        {
            // Keep latency low for editor preview use-cases without running our own update thread.
            Bass.UpdatePeriod = UpdatePeriodMs;
            Bass.PlaybackBufferLength = PlaybackBufferLengthMs;
            Bass.OggPreScan = true;
        }
        catch
        {
            // Config is best-effort; continue with defaults if unavailable.
        }

        if (!Bass.Init(Bass.DefaultDevice, OutputSampleRateHz, DeviceInitFlags.Default, IntPtr.Zero, IntPtr.Zero))
        {
            _lastBassError = Bass.LastError;
            return false;
        }

        _initialized = true;
        _lastBassError = Errors.OK;
        return true;
    }

    private int GetOrLoadHitsoundSample(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (_hitsoundSampleCache.TryGetValue(fullPath, out var existing))
        {
            return existing;
        }

        var sampleHandle = Bass.SampleLoad(fullPath, 0, 0, HitsoundSampleMaxVoices, BassFlags.Default);
        if (sampleHandle == 0)
        {
            _lastBassError = Bass.LastError;
            return 0;
        }

        _hitsoundSampleCache[fullPath] = sampleHandle;
        return sampleHandle;
    }

    private void CacheSongLength()
    {
        if (_songStreamHandle == 0)
        {
            _songLengthBytes = 0;
            _songDurationMs = 0;
            return;
        }

        _songLengthBytes = SafeChannelGetLengthBytes(_songStreamHandle);
        _songDurationMs = _songLengthBytes > 0 ? StreamBytesToMilliseconds(_songStreamHandle, _songLengthBytes) : 0;
    }

    private void ApplySongVolume()
    {
        if (_songStreamHandle == 0)
        {
            return;
        }

        _ = Bass.ChannelSetAttribute(_songStreamHandle, ChannelAttribute.Volume, _songVolume);
    }

    private void FreeSongStream()
    {
        if (_songStreamHandle != 0)
        {
            try
            {
                _ = Bass.StreamFree(_songStreamHandle);
            }
            catch
            {
                // Ignore free failures during reload/shutdown.
            }
        }

        _songStreamHandle = 0;
        _songPath = string.Empty;
        ResetLoadedSongRuntimeState();
    }

    private void ClearLoadedSongState()
    {
        _songPath = string.Empty;
        _songStreamHandle = 0;
        ResetLoadedSongRuntimeState();
    }

    private void ResetLoadedSongRuntimeState()
    {
        _songLengthBytes = 0;
        _songDurationMs = 0;
        ResetSongSeekDebugState();
    }

    private int GetObservedSongPositionMs(int streamHandle)
    {
        var positionBytes = SafeChannelGetPositionBytes(streamHandle);
        return StreamBytesToMilliseconds(streamHandle, positionBytes);
    }

    private static long MillisecondsToStreamBytes(int streamHandle, int timeMs)
    {
        var seconds = Math.Max(0, timeMs) / 1000d;
        var bytes = Bass.ChannelSeconds2Bytes(streamHandle, seconds);
        return bytes < 0 ? 0 : bytes;
    }

    private static int StreamBytesToMilliseconds(int streamHandle, long positionBytes)
    {
        if (positionBytes <= 0)
        {
            return 0;
        }

        var seconds = Bass.ChannelBytes2Seconds(streamHandle, positionBytes);
        if (seconds < 0)
        {
            return 0;
        }

        return (int)Math.Max(0, Math.Round(seconds * 1000d));
    }

    private static long SafeChannelGetLengthBytes(int streamHandle)
    {
        try
        {
            var length = Bass.ChannelGetLength(streamHandle, PositionFlags.Bytes);
            return length < 0 ? 0 : length;
        }
        catch
        {
            return 0;
        }
    }

    private static long SafeChannelGetPositionBytes(int streamHandle)
    {
        try
        {
            var position = Bass.ChannelGetPosition(streamHandle, PositionFlags.Bytes);
            return position < 0 ? 0 : position;
        }
        catch
        {
            return 0;
        }
    }

    private void UpdateLastSeekDebug(int targetTimeMs)
    {
        _lastSeekRequestMs = Math.Max(0, targetTimeMs);

        if (_songStreamHandle == 0)
        {
            _lastSeekObservedMs = -1;
            _lastSeekErrorMs = 0;
            return;
        }

        var observedMs = GetObservedSongPositionMs(_songStreamHandle);
        _lastSeekObservedMs = observedMs;
        _lastSeekErrorMs = observedMs - _lastSeekRequestMs;
    }

    private void ResetSongSeekDebugState()
    {
        _lastSeekRequestMs = -1;
        _lastSeekObservedMs = -1;
        _lastSeekErrorMs = 0;
    }

    private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
}
