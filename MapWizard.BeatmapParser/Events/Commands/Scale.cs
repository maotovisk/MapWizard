using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Scale : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Scale;

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
    public double? StartScale { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double? EndScale { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startScale"></param>
    /// <param name="endScale"></param>
    private Scale(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startScale,
        double? endScale
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartScale = startScale;
        EndScale = endScale;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Scale Decode(string line)
    {
        // _R,<easing>,<starttime>,<endtime>,<start_Scale>,<end_Scale>

        var commandSplit = line.Trim().Split(',');


        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        double? startScale = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null;
        double? endScale = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null;

        return new Scale(easing, startTime, endTime, startScale, endScale);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("S,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? "");
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? "");
        sb.Append(',');
        sb.Append(StartScale?.ToString(CultureInfo.InvariantCulture) ?? "");

        if (EndScale == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndScale?.ToString(CultureInfo.InvariantCulture) ?? "");
        return sb.ToString();
    }
}