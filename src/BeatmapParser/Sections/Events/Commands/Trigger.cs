using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Trigger : ICommand, ICommands
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Trigger;

    /// <summary>
    /// 
    /// </summary>
    public string TriggerType { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<ICommand> Commands { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="triggerType"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="commands"></param>
    private Trigger(string triggerType, TimeSpan startTime, TimeSpan endTime, List<ICommand> commands)
    {
        TriggerType = triggerType;
        StartTime = startTime;
        EndTime = endTime;
        Commands = commands;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="commandline"></param>
    /// <returns></returns>
    public static Trigger Decode(List<ICommand> parsedCommands, List<string> commands, int startindex)
    {
        // _T,(triggerType),(starttime),(endtime)

        var eventStartIndex = startindex + 1;
        var eventEndIndex = 0;

        List<ICommand> eventParsedCommands = [];

        for (var index = eventStartIndex; index != commands.Count; ++index)
        {
            if (!commands[index].StartsWith("  ") || !commands[index].StartsWith("  ")) break;

            eventParsedCommands.Add(Helper.ParseCommand(parsedCommands, commands, index));
            eventEndIndex = index;
        }

        // this is done to avoid the sub commands to be parsed again
        commands.RemoveRange(eventStartIndex, eventEndIndex);

        var commandSplit = commands[startindex].Split(',');
        return new Trigger
        (
            triggerType: commandSplit[1],
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])),
            commands: eventParsedCommands
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();
        builder.AppendLine($"T,{TriggerType},{StartTime},{EndTime}");

        foreach (var command in Commands)
        {
            builder.AppendLine(command.Encode());
        }
        return builder.ToString();
    }
}