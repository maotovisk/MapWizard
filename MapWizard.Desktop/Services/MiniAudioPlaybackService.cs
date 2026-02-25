using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Threading;
using MiniAudioEx.Core.StandardAPI;

namespace MapWizard.Desktop.Services;

public sealed class MiniAudioPlaybackService : IAudioPlaybackService, IDisposable
{
    private const uint SampleRate = 44100;
    private const uint Channels = 2;
    // Smaller period lowers output latency for editor-preview style transport.
    private const uint PeriodSizeInFrames = 256;

    private readonly object _sync = new();
    private readonly Dictionary<string, AudioClip> _clipCache = new(StringComparer.OrdinalIgnoreCase);

    private bool _initialized;
    private DispatcherTimer? _updateTimer;
    private AudioSource? _songSource;
    private AudioSource? _hitsoundSource;
    private AudioClip? _songClip;
    private string _songPath = string.Empty;

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
                _songClip = null;
                _songPath = string.Empty;
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

            var targetFrame = MillisecondsToFrames(startTimeMs, AudioContext.SampleRate);
            _songSource.Stop();
            _songSource.Cursor = 0;
            _songSource.Play(_songClip);
            AudioContext.Update();
            SetSongCursor(_songSource, targetFrame);

            // Streaming clips may not expose a seekable length/cursor immediately on the same tick.
            // Retry once after another update so transport seeks do not restart from 0.
            if (targetFrame > 0 && _songSource.Cursor == 0)
            {
                AudioContext.Update();
                SetSongCursor(_songSource, targetFrame);
            }

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

            return FramesToMilliseconds(_songSource.Cursor, AudioContext.SampleRate);
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
                    return 0;
                }

                _songSource.Stop();
                _songSource.Cursor = 0;
                _songSource.Play(_songClip);
                AudioContext.Update();

                var lengthFrames = _songSource.Length;
                _songSource.Stop();
                _songSource.Cursor = 0;

                return lengthFrames > 0
                    ? FramesToMilliseconds(lengthFrames, AudioContext.SampleRate)
                    : 0;
            }
            catch
            {
                return 0;
            }
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
            if (_hitsoundSource is not null)
            {
                _hitsoundSource.Volume = Clamp01(volume);
            }
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
            EnsureInitialized();

            if (_hitsoundSource is null)
            {
                return false;
            }

            var clip = GetOrCreateClip(filePath, streamFromDisk: false);
            if (clip is null)
            {
                return false;
            }

            _hitsoundSource.PlayOneShot(clip);
            return true;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _updateTimer?.Stop();
            _updateTimer = null;

            _songSource?.Dispose();
            _songSource = null;
            _hitsoundSource?.Dispose();
            _hitsoundSource = null;

            foreach (var clip in _clipCache.Values)
            {
                try
                {
                    clip.Dispose();
                }
                catch
                {
                    // Ignore clip disposal failures on shutdown.
                }
            }

            _clipCache.Clear();
            _songClip = null;
            _songPath = string.Empty;

            if (_initialized)
            {
                try
                {
                    AudioContext.Deinitialize();
                }
                catch
                {
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
        _hitsoundSource = new AudioSource(maxSources: 64);

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(2)
        };
        _updateTimer.Tick += (_, _) => AudioContext.Update();
        _updateTimer.Start();

        _initialized = true;
    }

    private AudioClip? GetOrCreateClip(string filePath, bool streamFromDisk)
    {
        var cacheKey = $"{(streamFromDisk ? "stream" : "mem")}::{Path.GetFullPath(filePath)}";
        if (_clipCache.TryGetValue(cacheKey, out var existing))
        {
            return existing;
        }

        try
        {
            var clip = new AudioClip(filePath, streamFromDisk);
            _clipCache[cacheKey] = clip;
            return clip;
        }
        catch
        {
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
        catch
        {
            // Fall through and try direct cursor set when length isn't available yet.
        }

        try
        {
            source.Cursor = targetFrame;
        }
        catch
        {
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
}
