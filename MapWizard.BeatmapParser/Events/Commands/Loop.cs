using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a Loop command within the beatmap configuration. It contains information
/// about the start time, the number of repetitions, and the associated nested commands.
/// </summary>
public class Loop : ICommand, IHasCommands
{
    /// <summary>
    /// Specifies the command type for the loop instance.
    /// This property is used to identify the loop as a specific command type
    /// within the MapWizard.BeatmapParser system.
    /// It is initialized to the default value of CommandType.Loop.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Loop;

    /// <summary>
    /// Represents the starting time of the loop command within a beatmap configuration.
    /// This property defines the initial point in time where the loop begins execution.
    /// It is expressed as a TimeSpan value.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Represents the number of repetitions for the Loop instance.
    /// This property determines how many times the associated nested commands
    /// should be executed during playback.
    /// </summary>
    public uint Count { get; set; }

    /// <summary>
    /// Represents a collection of commands associated with a specific loop or event.
    /// This property allows nested commands to be defined and managed within their
    /// respective context, such as loops, videos, or other complex structures.
    /// </summary>
    public List<ICommand> Commands { get; set; } = [];

    /// <summary>
    /// Represents a Loop command in the beatmap configuration, defining a set of repeated actions
    /// starting from a specified time. The loop has a start time, a number of iterations,
    /// and a list of commands to execute during each iteration.
    /// </summary>
    private Loop(TimeSpan startTime, uint count, List<ICommand> commands)
    {
        StartTime = startTime;
        Count = count;
        Commands = commands;
    }

    /// <summary>
    /// Decodes a string representation of a Loop command from the beatmap configuration
    /// into a Loop object. The string should follow the expected format,
    /// containing start time and the number of repetitions.
    /// </summary>
    /// <param name="line">The string representation of the Loop command to decode.</param>
    /// <returns>A <see cref="Loop"/> object representing the decoded Loop command.</returns>
    public static Loop Decode(string line)
    {
        // _L,(starttime),(loopcount)

        var commandSplit = line.Trim().Split(',');

        return new Loop
        (
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            count: uint.Parse(commandSplit[2]),
            commands: []
        );
    }

    /// <summary>
    /// Encodes the Loop instance into a string representation that complies with the osu! file format.
    /// The output string includes the start time of the loop, the number of iterations, and the encoded
    /// commands contained within the loop.
    /// </summary>
    /// <returns>A string that represents the loop command and its associated nested commands
    /// in the osu! file format.</returns>
    public string Encode()
    {
        if (Commands.Count == 0) return $"L,{StartTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},{Count}";

        StringBuilder builder = new();
        builder.AppendLine($"L,{StartTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},{Count}");

        foreach (var command in Commands[..^1])
        {
            builder.AppendLine(command is IHasCommands ? string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + command.Encode());
        }
        builder.AppendLine(Commands.Last() is IHasCommands ? string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + Commands.Last().Encode());

        return builder.ToString();
    }
}




