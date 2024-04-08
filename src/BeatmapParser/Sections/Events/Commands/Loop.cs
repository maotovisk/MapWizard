using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Loop : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Loop;

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public uint Count { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<ICommand> Commands { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="count"></param>
    /// <param name="commands"></param>
    private Loop(TimeSpan startTime, uint count, List<ICommand> commands)
    {
        StartTime = startTime;
        Count = count;
        Commands = commands;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Loop Decode(List<ICommand> parsedCommands, List<string> commands, int startindex)
    {
        // _L,(starttime),(loopcount)

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

        var commandSplit = commands[startindex].Trim().Split(',');
        return new Loop
        (
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            count: uint.Parse(commandSplit[4]),
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
        builder.AppendLine($"L,{StartTime},{Count}");

        foreach (var command in Commands)
        {
            builder.AppendLine(command.Encode());
        }
        return builder.ToString();
    }
}