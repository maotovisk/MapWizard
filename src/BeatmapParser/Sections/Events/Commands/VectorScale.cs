using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class VectorScale : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.VectorScale;

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
    public Vector2? StartVectorScale { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector2? EndVectorScale { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startVectorScale"></param>
    /// <param name="endVectorScale"></param>
    private VectorScale(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Vector2? startVectorScale,
        Vector2? endVectorScale
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartVectorScale = startVectorScale;
        EndVectorScale = endVectorScale;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static VectorScale Decode(string line)
    {
        // _V,(easing),(starttime),(endtime),(start_scale_x),(start_scale_y),(end_scale_x),(end_scale_y)

        var commandSplit = line.Trim().Split(',');

        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        Vector2? startVectorScale = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[4]) && !string.IsNullOrEmpty(commandSplit[5]) ?
            new Vector2(float.Parse(commandSplit[4], CultureInfo.InvariantCulture), float.Parse(commandSplit[5], CultureInfo.InvariantCulture)) : null;
        Vector2? endVectorScale = commandSplit.Length > 7 && !string.IsNullOrEmpty(commandSplit[6]) && !string.IsNullOrEmpty(commandSplit[7]) ?
            new Vector2(float.Parse(commandSplit[6], CultureInfo.InvariantCulture), float.Parse(commandSplit[7], CultureInfo.InvariantCulture)) : null;

        return new VectorScale(easing, startTime, endTime, startVectorScale, endVectorScale);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append($"V,{(int)Easing},{StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty},{EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");

        if (StartVectorScale != null) sb.Append($",{StartVectorScale?.X.ToString(CultureInfo.InvariantCulture)},{StartVectorScale?.Y.ToString(CultureInfo.InvariantCulture)}");
        else sb.Append(",,");

        if (EndVectorScale != null) sb.Append($",{EndVectorScale?.X.ToString(CultureInfo.InvariantCulture)},{EndVectorScale?.Y.ToString(CultureInfo.InvariantCulture)}");

        return sb.ToString();
    }
}