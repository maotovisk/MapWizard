
using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Video : IEvent, IHasCommands
{
    /// <summary>
    /// 
    /// </summary>
    public EventType Type { get; init; } = EventType.Video;

    /// <summary>
    ///  Time of the event.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector2? Offset { get; set; }


    public List<ICommand> Commands { get; set; }


    /// <summary>
    /// 
    /// </summary>
    private Video()
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        FilePath = string.Empty;
        Commands = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="offset"></param>
    private Video(string filename, Vector2? offset = null)
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        FilePath = filename;
        Offset = offset;
        Commands = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <param name="filename"></param>
    /// <param name="offset"></param>
    private Video(TimeSpan time, string filename, Vector2? offset = null)
    {
        StartTime = time;
        FilePath = filename;
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
        sb.Append($"{(int)Type},{StartTime.TotalMilliseconds},{FilePath}");
        if (Offset != null)
        {
            sb.Append(',');
            sb.Append(Offset.Value.X.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Offset.Value.Y.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
        }
        sb.AppendLine();
        if (Commands.Count == 0) sb.ToString();

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
    /// <returns></returns>
    public static Video Decode(string line)
    {
        // 1,0,filename,xOffset,yOffset
        try
        {
            var args = line.Trim().Split(',');
            return new Video
            (
                filename: args[2],
                time: TimeSpan.FromMilliseconds(double.Parse(args[1], CultureInfo.InvariantCulture)),
                offset: args.Length == 4 ? new Vector2(float.Parse(args[3], CultureInfo.InvariantCulture), float.Parse(args[4], CultureInfo.InvariantCulture)) : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Video line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}