using System.Globalization;
using System.Security.Cryptography.X509Certificates;
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
    public static Trigger Decode(string line)
    {
        // _T,(triggerType),(starttime),(endtime)

        var commandSplit = line.Trim().Split(',');

        return new Trigger
        (
            triggerType: commandSplit[1],
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])),
            commands: []
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();
        builder.AppendLine($"T,{TriggerType},{StartTime.Milliseconds.ToString(CultureInfo.InvariantCulture)},{EndTime.Milliseconds.ToString(CultureInfo.InvariantCulture)}");
        foreach (var command in Commands[..^1])
        {
            builder.AppendLine(string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)));
        }
        builder.Append(string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)));

        return builder.ToString();
    }
}