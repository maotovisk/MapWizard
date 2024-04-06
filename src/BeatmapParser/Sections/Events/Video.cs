
using System.Numerics;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Video : IEvent
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


    /// <summary>
    /// 
    /// </summary>
    private Video()
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        FilePath = string.Empty;
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
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        if (Offset == null) return $"0,0,{FilePath}";
        return $"{(int)EventTypes.Video},{StartTime},{FilePath},{Offset?.X},{Offset?.Y}";
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
                time: TimeSpan.FromMilliseconds(int.Parse(args[1])),
                offset: args.Length == 4 ? new Vector2(int.Parse(args[3]), int.Parse(args[4])) : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Video line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}