using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Fade : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Fade;

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
    public double? StartOpacity { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double? EndOpacity { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startOpacity"></param>
    /// <param name="endOpacity"></param>
    private Fade(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        double? startOpacity,
        double? endOpacity
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartOpacity = startOpacity;
        EndOpacity = endOpacity;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Fade Decode(string line)
    {
        // _F,(easing),(starttime),(endtime),(start_opacity),(end_opacity)

        var commandSplit = line.Trim().Split(',');

        return new Fade
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[1]),
            startTime: commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null,
            endTime: commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null,
            startOpacity: commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? double.Parse(commandSplit[4], CultureInfo.InvariantCulture) : null,
            endOpacity: commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[5]) ? double.Parse(commandSplit[5], CultureInfo.InvariantCulture) : null
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("F,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(StartOpacity?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        if (EndOpacity == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndOpacity?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        return sb.ToString();
    }
}