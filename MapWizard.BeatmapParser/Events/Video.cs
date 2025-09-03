
using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a video event in a beatmap file, containing information about
/// the video file, its start time, offset, and associated commands.
/// </summary>
public class Video : IEvent, IHasCommands
{
    /// <summary>
    /// Indicates the specific type of the event, as defined by the EventType enumeration.
    /// See <see cref="EventType"/> for more information.
    /// </summary>
    public EventType Type { get; init; } = EventType.Video;

    /// <summary>
    ///  Time of the event.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Represents the file path of the video associated with the event.
    /// This property contains the location of the video file as a string.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Represents the offset for the video in the beatmap file. This specifies the X and Y coordinates
    /// for positioning the video element on the screen, relative to a defined origin. A null value
    /// indicates that the default position should be used.
    /// </summary>
    public Vector2? Offset { get; set; }


    public List<ICommand> Commands { get; set; }


    /// <summary>
    /// Represents a video event in a beatmap file.
    /// This class handles the information related to a video event, including its type, start time, file path, offset, and associated commands.
    /// </summary>
    private Video()
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        FilePath = string.Empty;
        Commands = [];
    }

    /// <summary>
    /// Represents a video event in a beatmap file, encapsulating details such as type, start time,
    /// file path, offset, and any associated commands.
    /// </summary>
    private Video(string filename, Vector2? offset = null)
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        FilePath = filename;
        Offset = offset;
        Commands = [];
    }

    /// <summary>
    /// Represents a video event in a beatmap file, containing information about
    /// its type, start time, file path, offset, and associated commands.
    /// </summary>
    private Video(TimeSpan time, string filename, Vector2? offset = null)
    {
        StartTime = time;
        FilePath = filename;
        Offset = offset;
        Commands = [];
    }

    /// <summary>
    /// Encodes the current video event into a string representation suitable for a beatmap file.
    /// This includes the event type, start time, file path, optional offset values,
    /// and any associated commands, properly formatted for output.
    /// </summary>
    /// <returns>
    /// A string representing the encoded video event in the expected beatmap format.
    /// </returns>
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
        
        if (Commands.Count == 0) return sb.ToString();
        sb.AppendLine();

        foreach (var command in Commands[..^1])
        {
            sb.AppendLine(command is IHasCommands ? string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + command.Encode());
        }
        sb.AppendLine(Commands.Last() is IHasCommands ? string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + Commands.Last().Encode());

        return sb.ToString();
    }

    /// <summary>
    /// Decodes a string representation of a video event into a <see cref="Video"/> object.
    /// Parses the string to extract information such as the start time, file path, and optional offset values.
    /// </summary>
    /// <param name="line">The string containing the encoded video event data.</param>
    /// <returns>A <see cref="Video"/> object containing the decoded information.</returns>
    /// <exception cref="Exception">Thrown when the string cannot be parsed correctly due to invalid data format.</exception>
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