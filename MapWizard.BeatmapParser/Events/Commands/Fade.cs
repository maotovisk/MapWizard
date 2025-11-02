using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the Fade command in a beatmap, allowing for fade-in and fade-out effects
/// with specified easing, timing, and opacity values.
/// </summary>
public class Fade : ICommand
{
    private double? _startMilliseconds;
    private double? _endMilliseconds;

    /// <summary>
    /// Gets the specific type of the command, represented by the <see cref="CommandType"/> enumeration.
    /// This property identifies the nature of the command, such as Fade, Move, Scale, or other supported
    /// command types in the beatmap.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Fade;

    /// <summary>
    /// Specifies the easing type to be applied during the interpolation process
    /// within a command. The <see cref="Easing"/> enumeration determines the
    /// nature of transition, such as linear scaling or more complex easing
    /// curves like Bounce, Elastic, or Quadratic, affecting how a value
    /// progresses over time.
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Gets or sets the start time of the fade command in the beatmap.
    /// This property defines the timestamp at which the fade effect begins.
    /// </summary>
    public TimeSpan? StartTime
    {
        get => _startMilliseconds.HasValue ? TimeSpan.FromMilliseconds(_startMilliseconds.Value) : null;
        set => _startMilliseconds = value?.TotalMilliseconds;
    }

    /// <summary>
    /// Gets or sets the end time of the fade command. This property represents the time
    /// at which the fade effect concludes, measured in a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan? EndTime
    {
        get => _endMilliseconds.HasValue ? TimeSpan.FromMilliseconds(_endMilliseconds.Value) : null;
        set => _endMilliseconds = value?.TotalMilliseconds;
    }

    /// <summary>
    /// Gets or sets the starting opacity value for the fade effect.
    /// This property defines the initial transparency level of the object,
    /// where 0 represents fully transparent and 1 represents fully opaque.
    /// </summary>
    public double? StartOpacity { get; set; }

    /// <summary>
    /// Gets or sets the final opacity value of the fade effect in the beatmap.
    /// This property defines the opacity level at the end of the fade command,
    /// ranging from 0 (completely invisible) to 1 (fully visible).
    /// </summary>
    public double? EndOpacity { get; set; }

    /// <summary>
    /// Represents a command for applying a fade effect in a beatmap.
    /// The fade effect transitions the opacity of an object over time.
    /// </summary>
    private Fade(
        Easing easing,
        double? startMilliseconds,
        double? endMilliseconds,
        double? startOpacity,
        double? endOpacity
    )
    {
        Easing = easing;
        _startMilliseconds = startMilliseconds;
        _endMilliseconds = endMilliseconds;
        StartOpacity = startOpacity;
        EndOpacity = endOpacity;
    }

    /// <summary>
    /// Decodes a string representation of a fade command into a <see cref="Fade"/> object.
    /// The method parses the provided line to extract parameters like easing, time span, and opacity values.
    /// </summary>
    /// <param name="line">The string containing the encoded fade command. Expected format: "_F,(easing),(starttime),(endtime),(start_opacity),(end_opacity)".</param>
    /// <returns>A <see cref="Fade"/> object containing the parsed command parameters.</returns>
    public static Fade Decode(string line)
    {
        // _F,(easing),(starttime),(endtime),(start_opacity),(end_opacity)

        var commandSplit = line.Trim().Split(',');

        return new Fade
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[1]),
            startMilliseconds: commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? double.Parse(commandSplit[2], CultureInfo.InvariantCulture) : null,
            endMilliseconds: commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? double.Parse(commandSplit[3], CultureInfo.InvariantCulture) : null,
            startOpacity: commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null,
            endOpacity: commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null
        );
    }

    /// <summary>
    /// Encodes the properties of the <see cref="Fade"/> command into a string representation
    /// that complies with the osu! file format. The generated string includes information
    /// about easing type, timing, and opacity values for fade-in and fade-out effects.
    /// </summary>
    /// <returns>
    /// A string representation of the <see cref="Fade"/> command, formatted for use in beatmaps.
    /// </returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("F,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(_startMilliseconds?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(_endMilliseconds?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartOpacity?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        if (EndOpacity == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndOpacity?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        return sb.ToString();
    }
}
