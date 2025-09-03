using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a Sample event in a beatmap, implementing the IEvent interface.
/// </summary>
public class Sample : IEvent
{
    /// <summary>
    /// Gets the type of the event, represented as an <see cref="EventType"/>.
    /// </summary>
    public EventType Type { get; init; } = EventType.Sample;

    /// <summary>
    /// Gets or sets the start time of the sample event,
    /// represented as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Gets or sets the layer in which the sample event is positioned. Defaults to Background.
    /// See <see cref="Layer"/> for more information.
    /// </summary>
    public Layer Layer { get; set; }

    /// <summary>
    /// Gets or sets the file path associated with the sample event.
    /// Represents the location of the audio file used in the beatmap.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the volume of the sample event, represented as an integer value.
    /// </summary>
    public int Volume { get; set; }
    
    private Sample()
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        Layer = Layer.Background;
        FilePath = string.Empty;
        Volume = 100;
    }
    
    private Sample(
        TimeSpan startTime,
        Layer layer,
        string filePath,
        int volume)
    {
        StartTime = startTime;
        Layer = layer;
        FilePath = filePath;
        Volume = volume;
    }

    /// <summary>
    /// Encodes the sample event into a string representation following the osu! beatmap file format.
    /// </summary>
    /// <returns>A string that represents the encoded sample event, including its type, start time, layer, file path, and volume.</returns>
    public string Encode()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{(int)EventType.Sample}");
        sb.Append(',');
        sb.Append(StartTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
        sb.Append(',');
        sb.Append(Layer);
        sb.Append(',');
        sb.Append(FilePath);
        sb.Append(',');
        sb.Append(Volume);
        return sb.ToString();
    }

    /// <summary>
    /// Decodes a string representation of a sample event into a <see cref="Sample"/> object.
    /// </summary>
    /// <param name="line">The string representation of the sample event, typically in the osu! beatmap file format.</param>
    /// <returns>A <see cref="Sample"/> object parsed from the input string.</returns>
    /// <exception cref="Exception">Thrown when the input string does not match the expected format or contains invalid data.</exception>
    public static Sample Decode(string line)
    {
        // Sample,<time>,<layer_num>,"<filepath>",<volume>
        try
        {
            var args = line.Trim().Split(',');
            return new Sample
            (
                startTime: TimeSpan.FromMilliseconds(int.Parse(args[1])),
                layer: (Layer)Enum.Parse(typeof(Layer), args[2]),
                filePath: args[3],
                volume: args.Length > 4 ? int.Parse(args[4]) : 100
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Sample line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}