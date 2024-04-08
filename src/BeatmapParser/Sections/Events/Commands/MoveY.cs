using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class MoveY : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.MoveY;

    /// <summary>
    /// 
    /// </summary>
    public Easing Easing { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double? StartY { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double? EndY { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startY"></param>
    /// <param name="endY"></param>
    private MoveY(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startY,
        double? endY
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartY = startY;
        EndY = endY;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static MoveY Decode(string line)
    {
        //M,(easing),(starttime),(endtime),(start_Y),(end_Y)

        var commandSplit = line.Trim().Split(',');
        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        double? startY = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null;
        double? endY = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null;

        return new MoveY(easing, startTime, endTime, startY, endY);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("MY,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartY?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        if (EndY == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndY?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        return sb.ToString();
    }
}