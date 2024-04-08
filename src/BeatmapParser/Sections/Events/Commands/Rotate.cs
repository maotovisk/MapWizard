using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Rotate : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Rotate;

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
    public double? StartRotate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double? EndRotate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startRotate"></param>
    /// <param name="endRotate"></param>
    private Rotate(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startRotate,
        double? endRotate
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartRotate = startRotate;
        EndRotate = endRotate;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Rotate Decode(string line)
    {
        // _R,<easing>,<starttime>,<endtime>,<start_rotate>,<end_rotate>

        var commandSplit = line.Trim().Split(',');
        return new Rotate
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[1]),
            startTime: commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null,
            endTime: commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null,
            startRotate: commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null,
            endRotate: commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("R,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartRotate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (EndRotate == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndRotate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        return sb.ToString();
    }
}