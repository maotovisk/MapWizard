namespace BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Parameter : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Parameter;

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
    public ParameterName ParameterName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="easing"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="parameterName"></param>
    private Parameter(
        Easing easing,
        TimeSpan startTime,
        TimeSpan endTime,
        ParameterName parameterName
    )
    {
        Easing = easing;
        StartTime = startTime;
        EndTime = endTime;
        ParameterName = parameterName;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="parsedCommands"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Parameter Decode(IEvent result, List<ICommand> parsedCommands, string command)
    {
        // _F,(easing),(starttime),(endtime),(start_opacity),(end_opacity)
        
        var commandSplit = command.Trim().Split(',');
        return new Parameter
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[0]),
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            parameterName: (ParameterName)Enum.Parse(typeof(ParameterName), commandSplit[3])
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"P,{Easing},{StartTime},{EndTime},{(char)(int)ParameterName}";
    }
}