using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Trigger : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Trigger;

    /// <summary>
    /// 
    /// </summary>
    public object? TriggerType { get; set; }

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
    private Trigger(object? triggerType, TimeSpan startTime, TimeSpan endTime,List<ICommand> commands)
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
    /// <param name="command"></param>
    /// <returns></returns>
    public static Trigger Decode(IEvent result, List<ICommand> parsedCommands, string command)
    {
        // _T,(triggerType),(starttime),(endtime)

        var commandSplit = command.Trim().Split(',');
        return new Trigger
        (
            triggerType: null, // TODO
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])),
            commands: [] // TODO
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