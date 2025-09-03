using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Defines a colour transformation command in a beatmap, supporting properties such as easing, timing, and color values.
/// </summary>
public class Colour : ICommand
{
    /// <summary>
    /// Specifies the type of a command within the beatmap, identifying its category
    /// or the specific kind of transformation it performs.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Colour;

    /// <summary>
    /// Defines the interpolation behavior between keyframes in an animation, specifying
    /// how the transition progresses over time through various predefined easing functions.
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Represents the start time of the colour transformation command within the beatmap,
    /// indicating the point in time at which the transformation begins.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Represents the optional end time of the colour transformation command, indicating
    /// when the transformation is completed within the beatmap timeline.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Represents the initial color value used in a color transformation command,
    /// defining the starting point for the color transition within the beatmap.
    /// </summary>
    public Color? StartColour { get; set; }

    /// <summary>
    /// Represents the target colour at the end time of the colour transformation command in a beatmap.
    /// This property defines the final RGB values the object transitions to during the command duration.
    /// </summary>
    public Color? EndColour { get; set; }

    /// <summary>
    /// Represents a colour command in a beatmap with specific easing and timing characteristics.
    /// </summary>
    private Colour(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Color? startColour,
        Color? endColour
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartColour = startColour;
        EndColour = endColour;
    }

    /// <summary>
    /// Decodes a given line of text into a <see cref="Colour"/> command with its associated easing, timing, and colour parameters.
    /// </summary>
    /// <param name="line">The line of text representing the colour command in the beatmap file format.</param>
    /// <returns>A <see cref="Colour"/> object initialized with easing, timing, and colour data parsed from the provided line of text.</returns>
    public static Colour Decode(string line)
    {
        // _C,(easing),(starttime),(endtime),(start_r),(start_g),(start_b),(end_r),(end_g),(end_b)

        var commandSplit = line.Trim().Split(',');

        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        Color? startColour = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? Helper.ParseColorFromUnknownString(string.Join(',', commandSplit[4], commandSplit[5], commandSplit[6])) : null;
        Color? endColour = commandSplit.Length > 7 && !string.IsNullOrEmpty(commandSplit[7]) ? Helper.ParseColorFromUnknownString(string.Join(',', commandSplit[7], commandSplit[8], commandSplit[9])) : null;

        return new Colour(easing, startTime, endTime, startColour, endColour);
    }

    /// <summary>
    /// Encodes the colour transformation command into a formatted string that adheres to the osu! file format specification.
    /// </summary>
    /// <returns>A string representation of the colour command, including easing, timing, and color values.</returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append($"C,{(int)Easing},{StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty},{EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");

        if (StartColour != null) sb.Append($",{StartColour?.R},{StartColour?.G},{StartColour?.B}");
        else sb.Append(",,,");

        if (EndColour != null) sb.Append($",{EndColour?.R},{EndColour?.G},{EndColour?.B}");

        return sb.ToString();

    }
}