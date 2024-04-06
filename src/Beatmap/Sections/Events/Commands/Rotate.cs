using System.Globalization;

namespace BeatmapParser;

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
    public TimeSpan StartTime { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double StartRotate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double EndRotate { get; set; }
    
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
        TimeSpan startTime,
        TimeSpan endTime,
        double startRotate,
        double endRotate
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
    public static Rotate Decode(IEvent result, List<ICommand> parsedCommands, string command)
    {
        // _R,<easing>,<starttime>,<endtime>,<start_rotate>,<end_rotate>
        
        var commandSplit = command.Trim().Split(',');
        return new Rotate
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[0]),
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            startRotate: double.Parse(commandSplit[3],CultureInfo.InvariantCulture),
            endRotate: double.Parse(commandSplit[4],CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"R,{Easing},{StartTime},{EndTime},{StartRotate},{EndRotate}";
    }
}