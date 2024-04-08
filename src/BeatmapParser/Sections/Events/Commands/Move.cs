using System.Numerics;

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
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector2 StartPosition { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector2 EndPosition { get; set; }

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
        TimeSpan startTime,
        TimeSpan endTime,
        Vector2 startPosition,
        Vector2 endPosition
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
    public static Move Decode(List<ICommand> parsedCommands, List<string> commands, int commandindex)
    {
        //M,(easing),(starttime),(endtime),(start_x),(start_y),(end_x),(end_y)

        var commandSplit = commands[commandindex].Trim().Split(',');
        return new Move
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[0]),
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            startPosition: new Vector2(int.Parse(commandSplit[3]), int.Parse(commandSplit[4])),
            endPosition: new Vector2(int.Parse(commandSplit[5]), int.Parse(commandSplit[6]))
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"M,{Easing},{StartTime},{EndTime},{StartPosition.X},{StartPosition.Y},{EndPosition.X},{EndPosition.Y}";
    }
}