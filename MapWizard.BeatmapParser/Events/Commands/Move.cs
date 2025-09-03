using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a move command in the beatmap parser.
/// This command specifies the motion or transformation
/// from a start position to an end position over a period of time,
/// with an easing function determining the interpolation.
/// </summary>
public class Move : ICommand
{
    /// <summary>
    /// Defines the type of the command associated with the <see cref="Move"/> class.
    /// This property is initialized to the value <see cref="CommandType.Move"/>,
    /// representing a move command in the beatmap parser.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Move;

    /// <summary>
    /// Specifies the easing function used to interpolate the motion or transformation
    /// associated with the <see cref="Move"/> command.
    /// The <see cref="Easing"/> enumeration determines the nature of transition,
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Specifies the start time of the command event represented as a nullable <see cref="TimeSpan"/>.
    /// This property denotes when the move command should begin execution within the beatmap timeline.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the move command.
    /// This property indicates the timestamp at which the motion or transformation
    /// reaches its final position, marking the conclusion of the specified time interval.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Specifies the starting position of the move command in a 2D coordinate system.
    /// This property denotes the initial coordinates of the motion before any transformations or interpolation occur.
    /// </summary>
    public Vector2? StartPosition { get; set; }

    /// <summary>
    /// Specifies the final position of the move command in a 2D coordinate system.
    /// This property represents the target destination towards which the transformation is applied.
    /// </summary>
    public Vector2? EndPosition { get; set; }

    /// <summary>
    /// Represents a movement command with specified easing, timing, and positional properties.
    /// </summary>
    private Move(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Vector2? startPosition,
        Vector2? endPosition
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartPosition = startPosition;
        EndPosition = endPosition;
    }

    /// <summary>
    /// Decodes a line of text into a <see cref="Move"/> command by parsing its easing, timing,
    /// and positional properties as specified in the input string.
    /// </summary>
    /// <param name="line">The input string representing the move command to decode.
    /// Expected format: "M,(easing),(starttime),(endtime),(start_x),(start_y),(end_x),(end_y)".</param>
    /// <returns>A new <see cref="Move"/> instance containing the easing, timing,
    /// and positional data extracted from the input string.</returns>
    public static Move Decode(string line)
    {
        var commandSplit = line.Trim().Split(',');

        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        Vector2? startPosition = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[4]) && !string.IsNullOrEmpty(commandSplit[5]) ?
            new Vector2(float.Parse(commandSplit[4], CultureInfo.InvariantCulture), float.Parse(commandSplit[5], CultureInfo.InvariantCulture)) : null;
        Vector2? endPosition = commandSplit.Length > 7 && !string.IsNullOrEmpty(commandSplit[6]) && !string.IsNullOrEmpty(commandSplit[7]) ?
            new Vector2(float.Parse(commandSplit[6], CultureInfo.InvariantCulture), float.Parse(commandSplit[7], CultureInfo.InvariantCulture)) : null;

        return new Move(easing, startTime, endTime, startPosition, endPosition);
    }

    /// <summary>
    /// Encodes the Move command into a string representation that complies with the osu! file format.
    /// The output string contains the command type, easing, timing, and positional attributes where applicable.
    /// </summary>
    /// <returns>
    /// A string representing the Move command, including its type, easing function, start time, end time,
    /// and start and end positions if specified.
    /// </returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("M,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (StartPosition != null)
        {
            sb.Append(',');
            sb.Append(StartPosition?.X.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            sb.Append(',');
            sb.Append(StartPosition?.Y.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }
        else if (EndPosition != null)
        {
            sb.Append(",,");
        }

        if (EndPosition == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndPosition?.X.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndPosition?.Y.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        return sb.ToString();
    }
}