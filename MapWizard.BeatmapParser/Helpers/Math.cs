
namespace MapWizard.BeatmapParser;
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
        
        return TimeSpan.MinValue.TotalMilliseconds > value ? TimeSpan.MinValue : TimeSpan.FromMilliseconds(value);
    }


    /// <summary>
    /// Gets the end time of the slider.
    /// </summary>
    /// <returns></returns>
    public static TimeSpan CalculateEndTime(double sliderMultiplier, double beatLength, TimeSpan startTime, double pixelLength, int repeats)
    {
        return TimeSpan.FromMilliseconds(Math.Round(startTime.TotalMilliseconds + pixelLength / (100d * sliderMultiplier) * beatLength * repeats, MidpointRounding.ToEven));
    }
}