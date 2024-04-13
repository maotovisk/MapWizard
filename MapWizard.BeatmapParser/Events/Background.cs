
using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Background : IEvent, IHasCommands
{
    /// <summary>
    /// 
    /// </summary>
    public EventType Type { get; init; } = EventType.Background;

    /// <summary>
    ///  Time of the event.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector2? Offset { get; set; }

    public List<ICommand> Commands { get; set; }


    /// <summary>
    /// 
    /// </summary>
    private Background()
    {
        Filename = string.Empty;
        Commands = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="offset"></param>
    private Background(string filename, Vector2? offset = null)
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        Filename = filename;
        Offset = offset;
        Commands = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <param name="filename"></param>
    /// <param name="offset"></param>
    private Background(TimeSpan time, string filename, Vector2? offset = null)
    {
        StartTime = time;
        Filename = filename;
        Offset = offset;
        Commands = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();

        sb.Append($"{(int)Type},{StartTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},{Filename}");

        if (Offset != null) sb.Append($",{Offset?.X.ToString(CultureInfo.InvariantCulture)},{Offset?.Y.ToString(CultureInfo.InvariantCulture)}");

        if (Commands.Count < 1) return sb.ToString();
        sb.AppendLine();

        foreach (var command in Commands[..^1])
        {
            sb.AppendLine(command is IHasCommands ? string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + command.Encode());
        }
        sb.AppendLine(Commands.Last() is IHasCommands ? string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + Commands.Last().Encode());

        return sb.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Background Decode(string line)
    {
        // 0,0,filename,xOffset,yOffset
        try
        {
            var args = line.Trim().Split(',');
            return new Background
            (
                filename: args[2],
                time: TimeSpan.FromMilliseconds(float.Parse(args[1], CultureInfo.InvariantCulture)),
                offset: args.Length >= 4 ? new Vector2(float.Parse(args[3], CultureInfo.InvariantCulture), float.Parse(args[4], CultureInfo.InvariantCulture)) : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Background line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}