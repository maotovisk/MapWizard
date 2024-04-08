using System.Globalization;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Scale : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Scale;

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
    public double StartScale { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double EndScale { get; set; }

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
        TimeSpan startTime,
        TimeSpan endTime,
        double startScale,
        double endScale
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
    public static Scale Decode(List<ICommand> parsedCommands, List<string> commands, int commandindex)
    {
        // _R,<easing>,<starttime>,<endtime>,<start_Scale>,<end_Scale>

        var commandSplit = commands[commandindex].Trim().Split(',');
        return new Scale
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[0]),
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            startScale: double.Parse(commandSplit[3], CultureInfo.InvariantCulture),
            endScale: double.Parse(commandSplit[4], CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"S,{Easing},{StartTime},{EndTime},{StartScale},{EndScale}";
    }
}