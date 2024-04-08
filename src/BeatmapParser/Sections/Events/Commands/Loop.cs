using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Loop : ICommand, ICommands
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
    public List<ICommand> Commands { get; set; } = [];

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
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();
        builder.AppendLine($"L,{StartTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},{Count}");

        foreach (var command in Commands[..^1])
        {
            builder.AppendLine(string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)));
        }
        builder.Append(string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)));
        return builder.ToString();
    }
}




