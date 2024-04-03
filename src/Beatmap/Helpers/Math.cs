
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
}