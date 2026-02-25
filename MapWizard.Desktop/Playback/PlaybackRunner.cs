using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Playback;

internal enum PlaybackRunnerStartResult
{
    Started,
    MissingSongAudio
}

internal sealed class PlaybackRunner(IAudioPlaybackService audioPlaybackService) : IDisposable
{
    private const int ClockSampleIntervalMs = 1;
    private const int UiUpdateIntervalMs = 8;
    private const int ClockWarmupWindowMs = 300;
    private const int ClockWarmupJumpToleranceMs = 150;

    private readonly object _sync = new();
    private readonly IAudioPlaybackService _audioPlaybackService = audioPlaybackService;

    private CancellationTokenSource? _runCts;
    private int _runIdSequence;
    private int _currentRunId;
    private int _timelineEndMs;
    private int _clockSampleTimeMs;
    private int _isRunning;
    private int _isPaused;

    private long _clockLoopLastTickMs;
    private int _clockLoopLastIntervalMs;
    private int _clockLoopMaxIntervalMs;
    private long _schedulerLoopLastTickMs;
    private int _schedulerLoopLastIntervalMs;
    private int _schedulerLoopMaxIntervalMs;
    private int _schedulerLastLagMs;
    private int _schedulerMaxLagMs;
    private long _uiLoopLastTickMs;
    private int _uiLoopLastIntervalMs;
    private int _uiLoopMaxIntervalMs;

    public bool IsRunning => Volatile.Read(ref _isRunning) != 0;
    public bool IsPaused => Volatile.Read(ref _isPaused) != 0;

    public int CurrentTimeMs
    {
        get
        {
            var timelineEndMs = Math.Max(0, Volatile.Read(ref _timelineEndMs));
            return Math.Clamp(Volatile.Read(ref _clockSampleTimeMs), 0, timelineEndMs);
        }
    }

    public PlaybackRunnerStartResult Start(
        string songPath,
        int startTimeMs,
        double timelineEndMs,
        IReadOnlyList<HitSoundVisualizerPoint> points,
        Action<Action> postToUi,
        Action<int> onCursorUpdatedOnUi,
        Action<int> onPlaybackCompletedOnUi,
        Action<HitSoundVisualizerPoint, CancellationToken> onPlayPoint)
    {
        var clampedTimelineEndMs = Math.Max(0, (int)Math.Ceiling(timelineEndMs));
        var clampedStartTimeMs = Math.Clamp(startTimeMs, 0, clampedTimelineEndMs);

        StopInternal(resetPausedState: false, stopSongPlayback: true);

        var songStarted = _audioPlaybackService.LoadSong(songPath) && _audioPlaybackService.PlaySong(clampedStartTimeMs);
        if (!songStarted)
        {
            Volatile.Write(ref _isRunning, 0);
            Volatile.Write(ref _isPaused, 0);
            Volatile.Write(ref _timelineEndMs, clampedTimelineEndMs);
            Volatile.Write(ref _clockSampleTimeMs, clampedStartTimeMs);
            ResetTimingTelemetry();
            return PlaybackRunnerStartResult.MissingSongAudio;
        }

        var playbackPoints = points
            .Where(x => x.TimeMs >= clampedStartTimeMs)
            .OrderBy(x => x.TimeMs)
            .ToList();

        Volatile.Write(ref _timelineEndMs, clampedTimelineEndMs);
        Volatile.Write(ref _clockSampleTimeMs, clampedStartTimeMs);
        ResetTimingTelemetry();
        Volatile.Write(ref _isRunning, 1);
        Volatile.Write(ref _isPaused, 0);

        var cts = new CancellationTokenSource();
        var runId = Interlocked.Increment(ref _runIdSequence);
        Volatile.Write(ref _currentRunId, runId);
        lock (_sync)
        {
            _runCts = cts;
        }

        _ = RunPlaybackAsync(
            runId,
            clampedStartTimeMs,
            cts,
            playbackPoints,
            postToUi,
            onCursorUpdatedOnUi,
            onPlaybackCompletedOnUi,
            onPlayPoint);

        return PlaybackRunnerStartResult.Started;
    }

    public int Pause()
    {
        var currentTimeMs = GetCurrentTimeMs();
        StopInternal(resetPausedState: false, stopSongPlayback: false);
        Volatile.Write(ref _isPaused, 1);
        return currentTimeMs;
    }

    public void Stop()
    {
        StopInternal(resetPausedState: true, stopSongPlayback: true);
    }

    public int GetCurrentTimeMs()
    {
        var timelineEndMs = Math.Max(0, Volatile.Read(ref _timelineEndMs));
        if (!IsRunning)
        {
            return Math.Clamp(CurrentTimeMs, 0, timelineEndMs);
        }

        return Math.Clamp(_audioPlaybackService.GetSongPositionMs(), 0, timelineEndMs);
    }

    public string GetTimingTelemetryStatus()
    {
        var state = IsRunning ? "running" : (IsPaused ? "paused" : "idle");
        var clockLast = Volatile.Read(ref _clockLoopLastIntervalMs);
        var clockMax = Volatile.Read(ref _clockLoopMaxIntervalMs);
        var uiLast = Volatile.Read(ref _uiLoopLastIntervalMs);
        var uiMax = Volatile.Read(ref _uiLoopMaxIntervalMs);
        var schedLast = Volatile.Read(ref _schedulerLoopLastIntervalMs);
        var schedMax = Volatile.Read(ref _schedulerLoopMaxIntervalMs);
        var schedLagLast = Volatile.Read(ref _schedulerLastLagMs);
        var schedLagMax = Volatile.Read(ref _schedulerMaxLagMs);

        return $"PlaybackRunner[{state}] clk {clockLast}/{clockMax}ms | ui {uiLast}/{uiMax}ms | sched {schedLast}/{schedMax}ms lag {schedLagLast}/{schedLagMax}ms";
    }

    public void Dispose()
    {
        Stop();
    }

    private async Task RunPlaybackAsync(
        int runId,
        int startTimeMs,
        CancellationTokenSource cts,
        IReadOnlyList<HitSoundVisualizerPoint> playbackPoints,
        Action<Action> postToUi,
        Action<int> onCursorUpdatedOnUi,
        Action<int> onPlaybackCompletedOnUi,
        Action<HitSoundVisualizerPoint, CancellationToken> onPlayPoint)
    {
        using var clockTickSignal = new SemaphoreSlim(0, 1);
        var completedNaturally = false;
        var clockLoopTask = RunClockLoopAsync(runId, startTimeMs, cts, clockTickSignal, postToUi, onCursorUpdatedOnUi);

        try
        {
            completedNaturally = await RunSchedulerLoopAsync(runId, cts, clockTickSignal, playbackPoints, onPlayPoint);
        }
        finally
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
                // Ignore cancellation races during restart/stop.
            }

            try
            {
                await clockLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping/restarting.
            }
        }

        if (!completedNaturally || !IsCurrentRun(runId, cts))
        {
            return;
        }

        StopInternal(resetPausedState: true, stopSongPlayback: true);
        var endTimeMs = Math.Max(0, Volatile.Read(ref _timelineEndMs));
        postToUi(() =>
        {
            if (runId != Volatile.Read(ref _currentRunId))
            {
                return;
            }

            onPlaybackCompletedOnUi(endTimeMs);
        });
    }

    private async Task RunClockLoopAsync(
        int runId,
        int startTimeMs,
        CancellationTokenSource cts,
        SemaphoreSlim clockTickSignal,
        Action<Action> postToUi,
        Action<int> onCursorUpdatedOnUi)
    {
        var token = cts.Token;
        var lastUiUpdateTicks = 0L;
        var warmupStartTickMs = Environment.TickCount64;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (!IsCurrentRun(runId, cts))
                {
                    return;
                }

                RecordIntervalTelemetry(ref _clockLoopLastTickMs, ref _clockLoopLastIntervalMs, ref _clockLoopMaxIntervalMs);
                var currentTimeMs = GetCurrentTimeMs();
                var lastPublishedTimeMs = CurrentTimeMs;
                var warmupElapsedMs = Environment.TickCount64 - warmupStartTickMs;
                if (warmupElapsedMs < ClockWarmupWindowMs)
                {
                    var isClearlyStaleWarmupReading =
                        Math.Abs(currentTimeMs - lastPublishedTimeMs) > ClockWarmupJumpToleranceMs &&
                        Math.Abs(currentTimeMs - startTimeMs) > ClockWarmupJumpToleranceMs;
                    if (isClearlyStaleWarmupReading)
                    {
                        currentTimeMs = lastPublishedTimeMs;
                    }
                }
                if (!IsCurrentRun(runId, cts))
                {
                    return;
                }

                Volatile.Write(ref _clockSampleTimeMs, currentTimeMs);
                SignalSingleTick(clockTickSignal);

                var uiTick = Environment.TickCount64;
                if (uiTick - lastUiUpdateTicks >= UiUpdateIntervalMs)
                {
                    lastUiUpdateTicks = uiTick;
                    postToUi(() =>
                    {
                        if (!IsCurrentRun(runId, cts))
                        {
                            return;
                        }

                        RecordIntervalTelemetry(ref _uiLoopLastTickMs, ref _uiLoopLastIntervalMs, ref _uiLoopMaxIntervalMs);
                        onCursorUpdatedOnUi(currentTimeMs);
                    });
                }

                await Task.Delay(ClockSampleIntervalMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop/restart.
        }
    }

    private async Task<bool> RunSchedulerLoopAsync(
        int runId,
        CancellationTokenSource cts,
        SemaphoreSlim clockTickSignal,
        IReadOnlyList<HitSoundVisualizerPoint> playbackPoints,
        Action<HitSoundVisualizerPoint, CancellationToken> onPlayPoint)
    {
        var token = cts.Token;
        var nextPointIndex = 0;
        var sawSongPlaying = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (!IsCurrentRun(runId, cts))
                {
                    return false;
                }

                RecordIntervalTelemetry(ref _schedulerLoopLastTickMs, ref _schedulerLoopLastIntervalMs, ref _schedulerLoopMaxIntervalMs);
                if (_audioPlaybackService.IsSongPlaying)
                {
                    sawSongPlaying = true;
                }

                var currentTimeMs = CurrentTimeMs;
                while (nextPointIndex < playbackPoints.Count && playbackPoints[nextPointIndex].TimeMs <= currentTimeMs)
                {
                    if (!IsCurrentRun(runId, cts))
                    {
                        return false;
                    }

                    var point = playbackPoints[nextPointIndex++];
                    var lagMs = Math.Max(0, currentTimeMs - point.TimeMs);
                    Volatile.Write(ref _schedulerLastLagMs, lagMs);
                    UpdateMaxTelemetry(ref _schedulerMaxLagMs, lagMs);
                    onPlayPoint(point, token);
                }

                var songEndedNaturally = sawSongPlaying && !_audioPlaybackService.IsSongPlaying;
                if (songEndedNaturally)
                {
                    break;
                }

                await clockTickSignal.WaitAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return !token.IsCancellationRequested;
    }

    private void StopInternal(bool resetPausedState, bool stopSongPlayback)
    {
        if (resetPausedState)
        {
            Volatile.Write(ref _isPaused, 0);
        }

        Volatile.Write(ref _isRunning, 0);

        CancellationTokenSource? cts;
        lock (_sync)
        {
            cts = _runCts;
            _runCts = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
                // Ignore cancellation failures during stop/restart.
            }
            finally
            {
                cts.Dispose();
            }
        }

        if (stopSongPlayback)
        {
            _audioPlaybackService.StopSong();
        }
        else
        {
            _audioPlaybackService.PauseSong();
        }
    }

    private bool IsCurrentRun(int runId, CancellationTokenSource cts)
    {
        lock (_sync)
        {
            return runId == Volatile.Read(ref _currentRunId) && ReferenceEquals(_runCts, cts);
        }
    }

    private void ResetTimingTelemetry()
    {
        Interlocked.Exchange(ref _clockLoopLastTickMs, 0);
        Volatile.Write(ref _clockLoopLastIntervalMs, 0);
        Volatile.Write(ref _clockLoopMaxIntervalMs, 0);
        Interlocked.Exchange(ref _schedulerLoopLastTickMs, 0);
        Volatile.Write(ref _schedulerLoopLastIntervalMs, 0);
        Volatile.Write(ref _schedulerLoopMaxIntervalMs, 0);
        Volatile.Write(ref _schedulerLastLagMs, 0);
        Volatile.Write(ref _schedulerMaxLagMs, 0);
        Interlocked.Exchange(ref _uiLoopLastTickMs, 0);
        Volatile.Write(ref _uiLoopLastIntervalMs, 0);
        Volatile.Write(ref _uiLoopMaxIntervalMs, 0);
    }

    private static void RecordIntervalTelemetry(ref long lastTickField, ref int lastIntervalField, ref int maxIntervalField)
    {
        var nowTickMs = Environment.TickCount64;
        var previousTickMs = Interlocked.Exchange(ref lastTickField, nowTickMs);
        if (previousTickMs <= 0)
        {
            return;
        }

        var intervalMs = (int)Math.Clamp(nowTickMs - previousTickMs, 0, int.MaxValue);
        Volatile.Write(ref lastIntervalField, intervalMs);
        UpdateMaxTelemetry(ref maxIntervalField, intervalMs);
    }

    private static void SignalSingleTick(SemaphoreSlim signal)
    {
        if (signal.CurrentCount == 0)
        {
            signal.Release();
        }
    }

    private static void UpdateMaxTelemetry(ref int target, int candidate)
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
}
