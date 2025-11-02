
using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a background event in a beatmap.
/// This is the event used to set the beatmap background image.
/// </summary>
public class Background : IEvent, IHasCommands
{
    private double _startMilliseconds;

    /// <summary>
    /// Specifies the type of the event. This property determines the category of the event,
    /// such as background, video, sprite, or other supported event types within the beatmap.
    /// </summary>
    public EventType Type { get; init; } = EventType.Background;

    /// <summary>
    /// The start time of the background event. It doesn't really is used for anything.
    /// </summary>
    public TimeSpan StartTime
    {
        get => TimeSpan.FromMilliseconds(_startMilliseconds);
        set => _startMilliseconds = value.TotalMilliseconds;
    }

    /// <summary>
    /// Represents the file name of the background image.
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// Represents the offset for the background in the beatmap file. Not really used for anything.
    /// </summary>
    public Vector2? Offset { get; set; }

    public List<ICommand> Commands { get; set; }


    /// <summary>
    /// Represents a background event in a beatmap.
    /// This event is used for setting the background image of the beatmap.
    /// </summary>
    private Background()
    {
        Filename = string.Empty;
        Commands = [];
    }

    /// <summary>
    /// Represents a background event in a beatmap.
    /// This event is used to define and encode the background image of a beatmap,
    /// along with its associated properties such as filename, offset, and commands.
    /// </summary>
    public Background(string filename, Vector2? offset = null)
    {
        _startMilliseconds = 0;
        Filename = filename;
        Offset = offset;
        Commands = [];
    }

    /// <summary>
    /// Represents a background event in a beatmap.
    /// This is the event used to set the beatmap background image.
    /// </summary>
    private Background(double timeMilliseconds, string filename, Vector2? offset = null)
    {
        _startMilliseconds = timeMilliseconds;
        Filename = filename;
        Offset = offset;
        Commands = [];
    }

    /// <summary>
    /// Encodes the background event and its associated commands into a string representation.
    /// The encoded string includes information such as the event type, start time, filename,
    /// optional offset, and encoded commands formatted for output.
    /// </summary>
    /// <returns>A string representation of the encoded background event.</returns>
    public string Encode()
    {
        StringBuilder sb = new();

        sb.Append($"{(int)Type},{_startMilliseconds.ToString(CultureInfo.InvariantCulture)},{Filename}");

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
    /// Decodes a line representing a background event in a beatmap
    /// and creates a <see cref="Background"/> instance from it.
    /// </summary>
    /// <param name="line">The raw string representing the background event, typically in the format "0,0,filename,xOffset,yOffset".</param>
    /// <returns>
    /// A <see cref="Background"/> object initialized with the parsed data from the input string.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when the input line cannot be successfully parsed into a valid <see cref="Background"/> object.
    /// </exception>
    public static Background Decode(string line)
    {
        try
        {
            var args = line.Trim().Split(',');
            var start = double.Parse(args[1], CultureInfo.InvariantCulture);
            return new Background
            (
                timeMilliseconds: start,
                filename: args[2],
                offset: args.Length >= 4 ? new Vector2(float.Parse(args[3], CultureInfo.InvariantCulture), float.Parse(args[4], CultureInfo.InvariantCulture)) : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Background line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}
