
namespace BeatmapParser;
/// <summary>
/// Helper class.
/// </summary>
public partial class Helper
{
    /// <summary>
    /// Clamp a beat length.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static TimeSpan ClampTimeSpan(double value)
    {
        if (TimeSpan.MaxValue.TotalMilliseconds < value) return TimeSpan.MaxValue;
        if (TimeSpan.MinValue.TotalMilliseconds > value) return TimeSpan.MinValue;

        return TimeSpan.FromMilliseconds(value);
    }


    /// <summary>
    /// Gets the end time of the slider.
    /// </summary>
    /// <returns></returns>
    public static TimeSpan CalculateEndTime(double sliderVelocity, double beatLength, TimeSpan startTime, double pixelLength, int repeats)
    {
        return TimeSpan.FromMilliseconds(startTime.TotalMilliseconds + (pixelLength / (repeats * 100 * sliderVelocity) * beatLength));
    }
}