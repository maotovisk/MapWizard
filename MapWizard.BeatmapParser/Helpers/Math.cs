using System.Globalization;

namespace MapWizard.BeatmapParser;
/// <summary>
/// Helper class.
/// </summary>
public partial class Helper
{
    /// <summary>
    /// Versão de formatação alvo para encode (14 = stable, 128 = lazer).
    /// </summary>
    public static int FormatVersion { get; set; } = 14;

    /// <summary>
    /// Formata valores de tempo (ms) conforme a versão alvo.
    /// v14: trunca para inteiro. v128: preserva alta precisão.
    /// </summary>
    public static string FormatTime(double milliseconds)
        => FormatVersion == 128
            ? milliseconds.ToString(CultureInfo.InvariantCulture)
            : Math.Truncate(milliseconds).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Formata coordenadas X/Y conforme a versão alvo.
    /// v14: arredonda para inteiro. v128: preserva alta precisão.
    /// </summary>
    public static string FormatCoord(float value)
        => FormatVersion == 128
            ? value.ToString(CultureInfo.InvariantCulture)
            : Math.Round(value).ToString(CultureInfo.InvariantCulture);

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
    /// Clamps a millisecond value to the valid <see cref="TimeSpan"/> range, preserving double precision.
    /// </summary>
    /// <param name="value">Value in milliseconds.</param>
    /// <returns>The original value clamped to <see cref="TimeSpan.MinValue"/> and <see cref="TimeSpan.MaxValue"/> bounds.</returns>
    public static double ClampMilliseconds(double value)
    {
        if (value > TimeSpan.MaxValue.TotalMilliseconds) return TimeSpan.MaxValue.TotalMilliseconds;
        if (value < TimeSpan.MinValue.TotalMilliseconds) return TimeSpan.MinValue.TotalMilliseconds;
        return value;
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
