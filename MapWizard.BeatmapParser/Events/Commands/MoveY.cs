using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a command to move an object along the Y-axis in a beatmap.
/// </summary>
public class MoveY : ICommand
{
    /// <summary>
    /// Gets the type of the command, which is MoveY.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.MoveY;

    /// <summary>
    /// Gets or sets the easing function used for the movement.
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Gets or sets the start time of the movement.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the movement.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the starting Y-coordinate of the movement.
    /// </summary>
    public double? StartY { get; set; }

    /// <summary>
    /// Gets or sets the ending Y-coordinate of the movement.
    /// </summary>
    public double? EndY { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveY"/> class.
    /// </summary>
    /// <param name="easing">The easing function used for the movement.</param>
    /// <param name="startTime">The start time of the movement.</param>
    /// <param name="endTime">The end time of the movement.</param>
    /// <param name="startY">The starting Y-coordinate of the movement.</param>
    /// <param name="endY">The ending Y-coordinate of the movement.</param>
    private MoveY(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startY,
        double? endY
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartY = startY;
        EndY = endY;
    }

    /// <summary>
    /// Decodes a string representation of a MoveY command into a <see cref="MoveY"/> object.
    /// </summary>
    /// <param name="line">The string representation of the command.</param>
    /// <returns>A <see cref="MoveY"/> object representing the command.</returns>
    public static MoveY Decode(string line)
    {
        // M,(easing),(starttime),(endtime),(start_Y),(end_Y)

        var commandSplit = line.Trim().Split(',');
        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        double? startY = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null;
        double? endY = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null;

        return new MoveY(easing, startTime, endTime, startY, endY);
    }

    /// <summary>
    /// Encodes the current <see cref="MoveY"/> object into its string representation.
    /// </summary>
    /// <returns>A string representation of the MoveY command.</returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("MY,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartY?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        if (EndY == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndY?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        return sb.ToString();
    }
}