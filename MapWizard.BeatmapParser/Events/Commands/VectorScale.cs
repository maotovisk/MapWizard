using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a vector scaling command within the context of a beatmap parser.
/// This command specifies the scaling of an object over time using a start and end vector.
/// </summary>
public class VectorScale : ICommand
{
    /// <summary>
    /// Represents the specific command type for the current instance of ICommand.
    /// Defines the behavior and purpose of the command, which in this case is set to VectorScale.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.VectorScale;

    /// <summary>
    /// Defines the easing function applied during a transition or animation in the command.
    /// Specifies the interpolation behavior, allowing for smooth, accelerated, or decelerated movements.
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Defines the starting time of the vector scaling command in a beatmap.
    /// This property represents the moment when the scaling transition begins, measured as a nullable TimeSpan.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Represents the optional end time of the vector scaling command.
    /// Determines the time at which the scaling operation concludes.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Defines the starting vector scale for a scaling operation.
    /// Represents the initial dimensions of an object in the form of a two-dimensional vector.
    /// </summary>
    public Vector2? StartVectorScale { get; set; }

    /// <summary>
    /// Represents the target scaling vector at the end time of the vector scaling command.
    /// Defines the final dimensions or scale of an object in 2D space as defined by the VectorScale command.
    /// </summary>
    public Vector2? EndVectorScale { get; set; }

    /// <summary>
    /// Represents a vector scale transformation in a beatmap, including information about easing type,
    /// start and end times, and optional start and end vector scales.
    /// </summary>
    private VectorScale(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Vector2? startVectorScale,
        Vector2? endVectorScale
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartVectorScale = startVectorScale;
        EndVectorScale = endVectorScale;
    }

    /// <summary>
    /// Decodes a string representation of a vector scale command into a <see cref="VectorScale"/> object.
    /// </summary>
    /// <param name="line">The string containing the vector scale command data, formatted as "_V,(easing),(starttime),(endtime),(start_scale_x),(start_scale_y),(end_scale_x),(end_scale_y)".</param>
    /// <returns>A <see cref="VectorScale"/> object containing the parsed easing type, start and end times, and optional start and end vector scales.</returns>
    public static VectorScale Decode(string line)
    {
        // _V,(easing),(starttime),(endtime),(start_scale_x),(start_scale_y),(end_scale_x),(end_scale_y)

        var commandSplit = line.Trim().Split(',');

        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        Vector2? startVectorScale = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[4]) && !string.IsNullOrEmpty(commandSplit[5]) ?
            new Vector2(float.Parse(commandSplit[4], CultureInfo.InvariantCulture), float.Parse(commandSplit[5], CultureInfo.InvariantCulture)) : null;
        Vector2? endVectorScale = commandSplit.Length > 7 && !string.IsNullOrEmpty(commandSplit[6]) && !string.IsNullOrEmpty(commandSplit[7]) ?
            new Vector2(float.Parse(commandSplit[6], CultureInfo.InvariantCulture), float.Parse(commandSplit[7], CultureInfo.InvariantCulture)) : null;

        return new VectorScale(easing, startTime, endTime, startVectorScale, endVectorScale);
    }

    /// <summary>
    /// Encodes the current instance of the VectorScale object into a string representation
    /// compliant with the osu! file format.
    /// </summary>
    /// <returns>A string representation of the VectorScale object, including its easing type,
    /// start and end times, and start and end vector scales if defined.</returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append($"V,{(int)Easing},{StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty},{EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");

        if (StartVectorScale != null) sb.Append($",{StartVectorScale?.X.ToString(CultureInfo.InvariantCulture)},{StartVectorScale?.Y.ToString(CultureInfo.InvariantCulture)}");
        else sb.Append(",,");

        if (EndVectorScale != null) sb.Append($",{EndVectorScale?.X.ToString(CultureInfo.InvariantCulture)},{EndVectorScale?.Y.ToString(CultureInfo.InvariantCulture)}");

        return sb.ToString();
    }
}