using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Colour : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandType Type { get; init; } = CommandType.Colour;

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
    public Vector3? StartColour { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector3? EndColour { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startColour"></param>
    /// <param name="endColour"></param>
    private Colour(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Vector3? startColour,
        Vector3? endColour
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartColour = startColour;
        EndColour = endColour;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Colour Decode(string line)
    {
        // _C,(easing),(starttime),(endtime),(start_r),(start_g),(start_b),(end_r),(end_g),(end_b)

        var commandSplit = line.Trim().Split(',');

        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        Vector3? startColour = commandSplit.Length > 4 && !string.IsNullOrEmpty(commandSplit[4]) ? Helper.ParseVector3FromUnknownString(string.Join(',', commandSplit[4], commandSplit[5], commandSplit[6])) : null;
        Vector3? endColour = commandSplit.Length > 7 && !string.IsNullOrEmpty(commandSplit[7]) ? Helper.ParseVector3FromUnknownString(string.Join(',', commandSplit[7], commandSplit[8], commandSplit[9])) : null;

        return new Colour(easing, startTime, endTime, startColour, endColour);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append($"C,{(int)Easing},{StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty},{EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");

        if (StartColour != null) sb.Append($",{StartColour?.X},{StartColour?.Y},{StartColour?.Z}");
        else sb.Append(",,,");

        if (EndColour != null) sb.Append($",{EndColour?.X},{EndColour?.Y},{EndColour?.Z}");

        return sb.ToString();

    }
}