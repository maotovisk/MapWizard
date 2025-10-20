using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a Trigger command that defines an action or group of actions
/// that occur between a specified start and end time within a beatmap.
/// </summary>
public class Trigger : ICommand, IHasCommands
{
    private double _startMilliseconds;
    private double _endMilliseconds;

    /// <summary>
    /// Gets the type of the command, which corresponds to the predefined
    /// command types in the <see cref="CommandType"/> enum. This property
    /// is initialized to <see cref="CommandType.Trigger"/> by default.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Trigger;

    /// <summary>
    /// Gets or sets the type of the trigger, which defines the specific action
    /// or behavior associated with the trigger within a beatmap. This property
    /// is represented as a string and is essential for identifying the nature
    /// of the defined trigger.
    /// </summary>
    public string TriggerType { get; set; }

    /// <summary>
    /// Gets or sets the starting time of the trigger command within the beatmap,
    /// represented as a <see cref="TimeSpan"/>. This property defines the exact
    /// time at which the actions defined by the trigger will begin.
    /// </summary>
    public TimeSpan StartTime
    {
        get => TimeSpan.FromMilliseconds(_startMilliseconds);
        set => _startMilliseconds = value.TotalMilliseconds;
    }

    /// <summary>
    /// Gets or sets the end time of the trigger, which specifies the time at which
    /// the associated actions or commands cease within a beatmap.
    /// </summary>
    public TimeSpan EndTime
    {
        get => TimeSpan.FromMilliseconds(_endMilliseconds);
        set => _endMilliseconds = value.TotalMilliseconds;
    }

    /// <summary>
    /// Gets or sets the collection of commands associated with this object.
    /// These commands define a sequence of actions or events that are executed
    /// as part of the parent command's behavior.
    /// </summary>
    public List<ICommand> Commands { get; set; }

    /// <summary>
    /// Represents a Trigger command that defines an action or group of actions
    /// occurring within a specific time frame in a beatmap.
    /// </summary>
    private Trigger(string triggerType, double startMilliseconds, double endMilliseconds, List<ICommand> commands)
    {
        TriggerType = triggerType;
        _startMilliseconds = startMilliseconds;
        _endMilliseconds = endMilliseconds;
        Commands = commands;
    }

    /// <summary>
    /// Decodes a line of text into a Trigger command by parsing its details,
    /// including trigger type, start time, end time, and associated commands.
    /// </summary>
    /// <param name="line">The line of text representing the Trigger command to be decoded.</param>
    /// <returns>A Trigger object populated with the parsed data from the input line.</returns>
    public static Trigger Decode(string line)
    {
        // _T,(triggerType),(starttime),(endtime)

        var commandSplit = line.Trim().Split(',');

        var start = double.Parse(commandSplit[2], CultureInfo.InvariantCulture);
        var end = double.Parse(commandSplit[3], CultureInfo.InvariantCulture);

        return new Trigger
        (
            triggerType: commandSplit[1],
            startMilliseconds: start,
            endMilliseconds: end,
            commands: []
        );
    }

    /// <summary>
    /// Converts the trigger command and its associated actions into a string representation
    /// compliant with the osu! file format. This includes the trigger type, start and end times,
    /// and any nested commands.
    /// </summary>
    /// <returns>
    /// A string that represents the trigger command, including its associated metadata and nested commands.
    /// </returns>
    public string Encode()
    {
        if (Commands.Count == 0) return $"T,{TriggerType},{_startMilliseconds.ToString(CultureInfo.InvariantCulture)},{_endMilliseconds.ToString(CultureInfo.InvariantCulture)}";

        StringBuilder builder = new();
        builder.AppendLine($"T,{TriggerType},{_startMilliseconds.ToString(CultureInfo.InvariantCulture)},{_endMilliseconds.ToString(CultureInfo.InvariantCulture)}");

        foreach (var command in Commands[..^1])
        {
            builder.AppendLine(command is IHasCommands ? string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + command.Encode());
        }

        builder.AppendLine(Commands.Last() is IHasCommands ? string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + Commands.Last().Encode());

        return builder.ToString();
    }
}
