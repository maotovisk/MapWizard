using System.Globalization;

namespace BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Fade : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Fade;

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
    public double StartOpacity { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public double EndOpacity { get; set; }

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
        TimeSpan startTime,
        TimeSpan endTime,
        double startOpacity,
        double endOpacity
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
    public static Fade Decode(IEvent result, List<ICommand> parsedCommands, string command)
    {
        // _F,(easing),(starttime),(endtime),(start_opacity),(end_opacity)

        var commandSplit = command.Trim().Split(',');
        return new Fade
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[0]),
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            startOpacity: double.Parse(commandSplit[3], CultureInfo.InvariantCulture),
            endOpacity: double.Parse(commandSplit[4], CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"F,{Easing},{StartTime},{EndTime},{StartOpacity},{EndOpacity}";
    }
}