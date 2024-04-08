using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Sample : IEvent
{
    /// <summary>
    /// 
    /// </summary>
    public EventType Type { get; init; } = EventType.Sample;

    /// <summary>
    ///  Time of the event.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    ///  Time of the event.
    /// </summary>
    public Layer Layer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int Volume { get; set; }

    /// <summary>
    /// 
    /// </summary>
    private Sample()
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        Layer = Layer.Background;
        FilePath = string.Empty;
        Volume = 100;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="layer"></param>
    /// <param name="filePath"></param>
    /// <param name="volume"></param>
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
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{(int)EventTypes.Sample}");
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
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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
                volume: int.Parse(args[4])
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Sample line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}