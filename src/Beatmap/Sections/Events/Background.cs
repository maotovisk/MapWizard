
using System.Numerics;

namespace BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Background : IEvent
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


    /// <summary>
    /// 
    /// </summary>
    private Background()
    {
        Filename = string.Empty;
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
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        if (Offset == null) return $"0,0,{Filename}";
        return $"{(int)EventTypes.Background},{StartTime},{Filename},{Offset?.X},{Offset?.Y}";
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
                time: TimeSpan.FromMilliseconds(int.Parse(args[1])),
                offset: args.Length == 4 ? new Vector2(int.Parse(args[3]), int.Parse(args[4])) : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Background line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}