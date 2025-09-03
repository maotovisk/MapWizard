
namespace MapWizard.BeatmapParser;
/// <summary>
/// Helper class.
/// </summary>
public partial class Helper
{
    /// <summary>
    /// Clamps a millisecond value to the valid <see cref="TimeSpan"/> range.
    /// </summary>
    /// <param name="value">The value in milliseconds to clamp.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the clamped value.</returns>
    public static TimeSpan ClampTimeSpan(double value)
    {
        if (TimeSpan.MaxValue.TotalMilliseconds < value) return TimeSpan.MaxValue;
        
        return TimeSpan.MinValue.TotalMilliseconds > value ? TimeSpan.MinValue : TimeSpan.FromMilliseconds(value);
    }


    /// <summary>
    /// Calculates the end time of a slider given beatmap timing and slider parameters.
    /// </summary>
    /// <param name="sliderMultiplier">The slider velocity multiplier from the beatmap difficulty settings.</param>
    /// <param name="beatLength">The uninherited beat length in milliseconds at the slider's start time.</param>
    /// <param name="startTime">The start time of the slider.</param>
    /// <param name="pixelLength">The slider's path length in pixels.</param>
    /// <param name="repeats">The number of repeats (1 for no repeats).</param>
    /// <returns>The absolute end time of the slider as a <see cref="TimeSpan"/>.</returns>
    public static TimeSpan CalculateEndTime(double sliderMultiplier, double beatLength, TimeSpan startTime, double pixelLength, int repeats)
    {
        return TimeSpan.FromMilliseconds(Math.Round(startTime.TotalMilliseconds + pixelLength / (100d * sliderMultiplier) * beatLength * repeats, MidpointRounding.ToEven));
    }
}