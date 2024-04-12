using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Move : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Move;

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
    public Vector2? StartPosition { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector2? EndPosition { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    private Move(
        Easing easing,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Vector2? startPosition,
        Vector2? endPosition
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        StartPosition = startPosition;
        EndPosition = endPosition;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Move Decode(string line)
    {
        //M,(easing),(starttime),(endtime),(start_x),(start_y),(end_x),(end_y)
        //M,19,266294,269294,320,240,280.61,24

        var commandSplit = line.Trim().Split(',');

        Easing easing = commandSplit.Length > 1 ? (Easing)Enum.Parse(typeof(Easing), commandSplit[1]) : Easing.Linear;
        TimeSpan? startTime = commandSplit.Length > 2 && !string.IsNullOrEmpty(commandSplit[2]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])) : null;
        TimeSpan? endTime = commandSplit.Length > 3 && !string.IsNullOrEmpty(commandSplit[3]) ? TimeSpan.FromMilliseconds(int.Parse(commandSplit[3])) : null;
        Vector2? startPosition = commandSplit.Length > 5 && !string.IsNullOrEmpty(commandSplit[4]) && !string.IsNullOrEmpty(commandSplit[5]) ?
            new Vector2(float.Parse(commandSplit[4], CultureInfo.InvariantCulture), float.Parse(commandSplit[5], CultureInfo.InvariantCulture)) : null;
        Vector2? endPosition = commandSplit.Length > 7 && !string.IsNullOrEmpty(commandSplit[6]) && !string.IsNullOrEmpty(commandSplit[7]) ?
            new Vector2(float.Parse(commandSplit[6], CultureInfo.InvariantCulture), float.Parse(commandSplit[7], CultureInfo.InvariantCulture)) : null;

        return new Move(easing, startTime, endTime, startPosition, endPosition);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();
        sb.Append("M,");
        sb.Append((int)Easing);
        sb.Append(',');
        sb.Append(StartTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndTime?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (StartPosition != null)
        {
            sb.Append(',');
            sb.Append(StartPosition?.X.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            sb.Append(',');
            sb.Append(StartPosition?.Y.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }
        else if (EndPosition != null)
        {
            sb.Append(",,");
        }

        if (EndPosition == null) return sb.ToString();

        sb.Append(',');
        sb.Append(EndPosition?.X.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        sb.Append(',');
        sb.Append(EndPosition?.Y.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

        return sb.ToString();
    }
}