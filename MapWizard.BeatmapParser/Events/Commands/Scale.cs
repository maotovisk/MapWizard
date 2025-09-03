using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a scale transformation command within a beatmap.
/// This command adjusts the scale of an object over a defined time interval with optional easing.
/// </summary>
public class Scale : ICommand
{
    /// <summary>
    /// Gets the type of the command represented by this instance.
    /// This property is used to specify the behavior or action
    /// associated with the command in the <see cref="CommandType"/> enumeration.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Scale;

    /// <summary>
    /// Gets or sets the easing function applied to the scale transition.
    /// This property defines how the scaling animation progresses over time,
    /// based on the selected easing type from the <see cref="Easing"/> enumeration.
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Gets or sets the starting time of the scale transformation command.
    /// This property defines the point in time at which the scale adjustment begins,
    /// represented as a nullable <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the scale transformation command.
    /// This property defines the time at which the scaling operation finishes.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the starting scale value of the transformation.
    /// This property defines the initial scale of the object when the scale command begins.
    /// </summary>
    public double? StartScale { get; set; }

    /// <summary>
    /// Gets or sets the final scale value for the scale transformation command.
    /// This property represents the target scale that the object transitions to
    /// during the specified time interval.
    /// </summary>
    public double? EndScale { get; set; }

    /// <summary>
    /// Represents a scaling command that defines the scaling transformation
    /// over time for an element in a beatmap.
    /// </summary>
    private Scale(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startScale,
        double? endScale
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartScale = startScale;
        EndScale = endScale;
    }

    /// <summary>
    /// Decodes a string representation of a scaling command into a Scale object.
    /// This method parses the scaling transformation's easing, start time, end time,
    /// start scale, and end scale values from the input string.
    /// </summary>
    /// <param name="line">The string containing the encoded scale command in
    /// the format '_R,&lt;easing&gt;,&lt;starttime&gt;,&lt;endtime&gt;,&lt;start_Scale&gt;,&lt;end_Scale&gt;'.</param>
    /// <returns>A Scale object initialized with the parsed data from the input string.</returns>
    public static Scale Decode(string line)
    {
        // _R,<easing>,<starttime>,<endtime>,<start_Scale>,<end_Scale>

        var commandSplit = line.Trim().Split(',');


        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        double? startScale = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null;
        double? endScale = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null;

        return new Scale(easing, startTime, endTime, startScale, endScale);
    }

    /// <summary>
    /// Encodes a scale transformation command into a string representation
    /// that adheres to the osu! file format specification.
    /// </summary>
    /// <returns>
    /// A string that represents the scale transformation command,
    /// including properties such as easing type, start time, end time,
    /// start scale, and optionally end scale.
    /// </returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("S,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? "");
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? "");
        sb.Append(',');
        sb.Append(StartScale?.ToString(CultureInfo.InvariantCulture) ?? "");

        if (EndScale == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndScale?.ToString(CultureInfo.InvariantCulture) ?? "");
        return sb.ToString();
    }
}