using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a Parameter command in the Beatmap parser. The Parameter command is used to
/// specify certain attributes such as blending modes or flipping behaviors, which can be
/// applied to beatmap elements.
/// </summary>
public class Parameter : ICommand
{
    /// <summary>
    /// Represents the specific type of command associated with the parameter.
    /// This property is initialized to the default value of <see cref="CommandType.Parameter"/>.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Parameter;

    /// <summary>
    /// Specifies the easing function applied to the interpolation of a parameter over time.
    /// This property is used to determine the rate of change for the motion, allowing for effects such as linear movement,
    /// ease-in, ease-out, or more complex easing patterns defined in the <see cref="MapWizard.BeatmapParser.Easing"/> enumeration.
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// Represents the starting time of the parameter command in a beatmap.
    /// This property is optional and specifies the time at which the parameter effect begins.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Represents the end time of a command in the beatmap, indicating when the effect
    /// or parameter transition associated with the command ends.
    /// This property is nullable, as not all commands require an explicit end time.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the name of the parameter being applied.
    /// This property determines the specific action, such as flipping or blending mode,
    /// associated with this parameter.
    /// </summary>
    public ParameterName ParameterName { get; set; }

    /// <summary>
    /// Represents a parameter in the beatmap parser, characterized by easing, start time, end time, and parameter name.
    /// </summary>
    private Parameter(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        ParameterName parameterName
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        ParameterName = parameterName;
    }

    /// <summary>
    /// Decodes a provided string into a <see cref="Parameter"/> object by parsing the string's components.
    /// </summary>
    /// <param name="line">The string containing encoded data for a parameter, specifying easing, timing, and parameter type. </param>
    /// <returns>A <see cref="Parameter"/> object representing the parsed data from the input string.</returns>
    public static Parameter Decode(string line)
    {
        var commandSplit = line.Trim().Split(',');

        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        ParameterName parameterName = commandSplit[4].Last() switch
        {
            'A' => ParameterName.AdditiveBlending,
            'H' => ParameterName.FlipHorizontal,
            'V' => ParameterName.FlipVertical,
            _ => throw new Exception($"Invalid parameter name {commandSplit[4]} in line {line}")
        };


        return new Parameter(easing, startTime, endTime, parameterName);
    }

    /// <summary>
    /// Encodes the Parameter command into a string representation that conforms to the osu! file format.
    /// The resulting string includes details such as easing type, start time, end time, and parameter name.
    /// </summary>
    /// <returns>A string representation of the Parameter command.</returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("P,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append((char)(int)ParameterName);
        return sb.ToString();
    }
}