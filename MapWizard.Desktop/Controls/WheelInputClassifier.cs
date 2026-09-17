using System;
using System.Diagnostics;

namespace MapWizard.Desktop.Controls;

/// <summary>
/// Distinguishes discrete mouse-wheel input from precision scrolling as far as
/// Avalonia's wheel event API allows. Fractional and horizontal deltas identify
/// a precision gesture. Once identified, occasional integer deltas remain direct
/// for the rest of that gesture without misclassifying rapid mouse-wheel detents.
/// </summary>
internal sealed class WheelInputClassifier
{
    private static readonly long SessionGapTicks = ToStopwatchTicks(TimeSpan.FromMilliseconds(180));

    private long _lastEventTimestamp;
    private bool _isPrecisionSession;

    public bool IsPrecisionInput(double deltaX, double deltaY)
    {
        var now = Stopwatch.GetTimestamp();
        var elapsed = _lastEventTimestamp == 0 ? long.MaxValue : now - _lastEventTimestamp;

        if (elapsed > SessionGapTicks)
        {
            _isPrecisionSession = false;
        }

        var hasFractionalDelta = !IsWholeDetent(deltaX) || !IsWholeDetent(deltaY);
        var hasHorizontalDelta = Math.Abs(deltaX) >= 0.001d;
        if (hasFractionalDelta || hasHorizontalDelta)
        {
            _isPrecisionSession = true;
        }

        _lastEventTimestamp = now;
        return _isPrecisionSession;
    }

    public void Reset()
    {
        _lastEventTimestamp = 0;
        _isPrecisionSession = false;
    }

    private static bool IsWholeDetent(double value) =>
        Math.Abs(value - Math.Round(value)) < 0.001d;

    private static long ToStopwatchTicks(TimeSpan duration) =>
        (long)(duration.TotalSeconds * Stopwatch.Frequency);
}
