using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class MoveX : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandType Type { get; init; } = CommandType.MoveX;

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
    public double? StartX { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double? EndX { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startX"></param>
    /// <param name="endX"></param>
    private MoveX(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startX,
        double? endX
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartX = startX;
        EndX = endX;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static MoveX Decode(string line)
    {
        //M,(easing),(starttime),(endtime),(start_x),(end_x)

        var commandSplit = line.Trim().Split(',');
        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        double? startX = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null;
        double? endX = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null;

        return new MoveX(easing, startTime, endTime, startX, endX);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("MX,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartX?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        if (EndX == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndX?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        return sb.ToString();
    }
}