using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a horizontal movement command (MoveX) within a beatmap. This class is used for parsing,
/// manipulating, and encoding horizontal movement commands in a specific time period with starting
/// and ending X-coordinates.
/// </summary>
public class MoveX : ICommand
{
    private double? _startMilliseconds;
    private double? _endMilliseconds;

    /// <summary>
    /// Gets the type of command associated with this instance.
    /// This property specifies a unique identifier for the particular command within a set of defined command types.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.MoveX;

    /// <summary>
    /// Specifies the easing function used to interpolate the motion or transformation
    /// associated with the <see cref="Move"/> command.
    /// The <see cref="Easing"/> enumeration determines the nature of transition,
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Gets or sets the start time of the horizontal movement command.
    /// This property represents the time at which the movement begins,
    /// expressed as a nullable <see cref="TimeSpan"/> for flexibility in defining a specific time point.
    /// </summary>
    public TimeSpan? StartTime
    {
        get => _startMilliseconds.HasValue ? TimeSpan.FromMilliseconds(_startMilliseconds.Value) : null;
        set => _startMilliseconds = value?.TotalMilliseconds;
    }

    /// <summary>
    /// Gets or sets the ending time of the horizontal movement command.
    /// This property defines the point in time when the movement concludes, typically measured from the start
    /// of the beatmap timeline. A null value indicates that the ending time is not explicitly defined.
    /// </summary>
    public TimeSpan? EndTime
    {
        get => _endMilliseconds.HasValue ? TimeSpan.FromMilliseconds(_endMilliseconds.Value) : null;
        set => _endMilliseconds = value?.TotalMilliseconds;
    }

    /// <summary>
    /// Gets or sets the starting X-coordinate for the horizontal movement.
    /// This property represents the initial position of the movement along the X-axis
    /// when the command begins execution.
    /// </summary>
    public double? StartX { get; set; }

    /// <summary>
    /// Gets or sets the ending X-coordinate for the horizontal movement command.
    /// This property represents the target position along the X-axis that the movement should reach
    /// at the end of the specified time period.
    /// </summary>
    public double? EndX { get; set; }

    /// <summary>
    /// Represents a command to move an object along the X-axis in a beatmap, with configurable easing type,
    /// timing, and positional details.
    /// </summary>
    private MoveX(
        Easing easing,
        double? startMilliseconds,
        double? endMilliseconds,
        double? startX,
        double? endX
    )
    {
        Easing = easing;
        _startMilliseconds = startMilliseconds;
        _endMilliseconds = endMilliseconds;
        StartX = startX;
        EndX = endX;
    }

    /// <summary>
    /// Decodes a string representation of a MoveX command and constructs a new <see cref="MoveX"/> object
    /// populated with the easing type, optional start and end times, and optional start and end X-coordinates.
    /// </summary>
    /// <param name="line">
    /// A string containing the formatted command to parse, expected to follow the osu! beatmap file conventions
    /// for MoveX commands.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="MoveX"/> class populated with parsed easing type, start and end times,
    /// and start and end X-coordinates.
    /// </returns>
    public static MoveX Decode(string line)
    {
        //M,(easing),(starttime),(endtime),(start_x),(end_x)

        var commandSplit = line.Trim().Split(',');
        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        double? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? double.Parse(commandSplit[2], CultureInfo.InvariantCulture) : null;
        double? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? double.Parse(commandSplit[3], CultureInfo.InvariantCulture) : null;
        double? startX = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null;
        double? endX = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null;

        return new MoveX(easing, startTime, endTime, startX, endX);
    }

    /// <summary>
    /// Encodes the current MoveX command into its string representation following the specified format.
    /// </summary>
    /// <returns>
    /// A string that represents the encoded MoveX command, including its easing type, optional start and end times,
    /// and optional start and end X-coordinates. The format complies with the osu! beatmap file conventions.
    /// </returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("MX,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(_startMilliseconds?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(_endMilliseconds?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartX?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        if (EndX == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndX?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        return sb.ToString();
    }
}
