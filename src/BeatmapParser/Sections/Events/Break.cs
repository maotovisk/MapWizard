using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Break : IEvent
{
    /// <summary>
    /// 
    /// </summary>
    public EventType Type { get; init; } = EventType.Break;

    /// <summary>
    ///  Time of the event.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    private Break(TimeSpan startTime, TimeSpan endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// 
    /// </summary>
    private Break()
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        EndTime = TimeSpan.FromMilliseconds(0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{(int)EventType.Break}");
        sb.Append(',');
        sb.Append(StartTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
        sb.Append(',');
        sb.Append(EndTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Break Decode(string line)
    {
        // 2,startTime,endTime

        try
        {
            var args = line.Trim().Split(',');
            return new Break
            (
                startTime: TimeSpan.FromMilliseconds(int.Parse(args[1])),
                endTime: TimeSpan.FromMilliseconds(int.Parse(args[2]))
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Break line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}