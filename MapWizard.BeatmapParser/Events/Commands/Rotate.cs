using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a rotate command in a beatmap parser.
/// This class processes and stores information about rotation transformations
/// applied to objects during a given time frame, using configurable easing options.
/// </summary>
public class Rotate : ICommand
{
    /// <summary>
    /// Gets the type of the command represented by this property.
    /// </summary>
    /// <remarks>
    /// The purpose of this property is to specify the command type associated
    /// with an implementation of the <see cref="ICommand"/> interface.
    /// The command type is determined using the <see cref="CommandType"/> enum,
    /// which defines various possible types of commands such as Rotate, Move,
    /// Scale, and others.
    /// </remarks>
    /// <value>
    /// Returns a <see cref="CommandType"/> value that indicates the specific
    /// type of the command.
    /// </value>
    public CommandType Type { get; init; } = CommandType.Rotate;

    /// <summary>
    /// Gets or sets the easing method applied to the command's transitions over time.
    /// </summary>
    /// <remarks>
    /// This property represents the smoothing function used to define the acceleration
    /// and deceleration of a command, such as Rotate, during its execution. The easing
    /// function determines the rate of change of the command's parameter(s) over time,
    /// allowing for linear, exponential, elastic, or other types of interpolations.
    /// </remarks>
    /// <value>
    /// Returns an <see cref="Easing"/> enumeration value indicating the specific method
    /// of easing (e.g., Linear, QuadInOut, ElasticOut, etc.) associated with the command.
    /// </value>
    public Easing Easing { get; set; }

    /// <summary>
    /// Gets or sets the start time of the rotation command.
    /// </summary>
    /// <remarks>
    /// This property specifies the time at which the rotation transformation begins
    /// within the context of a beatmap. It is measured as a <see cref="TimeSpan"/>
    /// value and can be null to indicate the absence of a defined start time.
    /// </remarks>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the time at which the rotation command
    /// starts, or null if no start time is defined.
    /// </value>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the rotation command.
    /// </summary>
    /// <remarks>
    /// This property specifies the time at which the rotation transformation ends in the beatmap.
    /// It works in conjunction with <see cref="StartTime"/> to define the duration of the rotation effect.
    /// The time is represented as a nullable <see cref="TimeSpan"/>, allowing for the possibility of
    /// undefined end time in certain scenarios.
    /// </remarks>
    /// <value>
    /// A <see cref="TimeSpan?"/> value that indicates the time, in milliseconds, at
    /// which the rotation command finishes. Returns null if no end time is specified.
    /// </value>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the initial rotation value for a rotate command.
    /// </summary>
    /// <remarks>
    /// This property represents the starting angle for an object's rotation in a rotate command.
    /// The value is typically a double that indicates the angle in degrees or radians,
    /// depending on the specific implementation or format. This property may be null to indicate
    /// the absence of an initial rotation value.
    /// </remarks>
    /// <value>
    /// A nullable <see cref="double"/> that specifies the starting rotation angle
    /// for the associated command.
    /// </value>
    public double? StartRotate { get; set; }

    /// <summary>
    /// Gets or sets the final rotation value for the rotate command.
    /// </summary>
    /// <remarks>
    /// This property specifies the rotation angle at the end of the transformation
    /// for objects affected by this command. The final rotation is applied over
    /// the duration defined by <see cref="StartTime"/> and <see cref="EndTime"/>,
    /// in combination with the specified <see cref="Easing"/> method.
    /// </remarks>
    /// <value>
    /// A <see cref="double"/> representing the final rotation value, in degrees.
    /// </value>
    public double? EndRotate { get; set; }

    /// <summary>
    /// Represents a Rotate command that defines a rotation operation with specific easing, timing, and rotation values.
    /// </summary>
    private Rotate(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startRotate,
        double? endRotate
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartRotate = startRotate;
        EndRotate = endRotate;
    }

    /// <summary>
    /// Decodes a line of text into a <see cref="Rotate"/> command by parsing its components and mapping them to the corresponding properties.
    /// </summary>
    /// <param name="line">The line of text representing the encoded Rotate command. Expected format: "_R,<easing>,<starttime>,<endtime>,<start_rotate>,<end_rotate>".</param>
    /// <returns>A <see cref="Rotate"/> object with its properties populated based on the parsed line.</returns>
    public static Rotate Decode(string line)
    {
        // _R,<easing>,<starttime>,<endtime>,<start_rotate>,<end_rotate>

        var commandSplit = line.Trim().Split(',');
        return new Rotate
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[1]),
            startTime: commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null,
            endTime: commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null,
            startRotate: commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null,
            endRotate: commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null
        );
    }

    /// <summary>
    /// Encodes the current Rotate command into a string representation
    /// compatible with the osu! file format, using specific properties
    /// such as easing, timing, and rotation values.
    /// </summary>
    /// <returns>A string that represents the encoded Rotate command.</returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("R,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartRotate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (EndRotate == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndRotate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        return sb.ToString();
    }
}