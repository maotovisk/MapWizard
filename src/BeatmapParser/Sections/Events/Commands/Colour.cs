using System.Numerics;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Colour : ICommand
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; } = CommandTypes.Colour;

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
    public Vector3 StartColour { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector3 EndColour { get; set; }

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
        TimeSpan startTime,
        TimeSpan endTime,
        Vector3 startColour,
        Vector3 endColour
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
    public static Colour Decode(List<ICommand> parsedCommands, List<string> commands, int commandindex)
    {
        // _C,(easing),(starttime),(endtime),(start_r),(start_g),(start_b),(end_r),(end_g),(end_b)

        var commandSplit = commands[commandindex].Trim().Split(',');
        return new Colour
        (
            easing: (Easing)Enum.Parse(typeof(Easing), commandSplit[0]),
            startTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[1])),
            endTime: TimeSpan.FromMilliseconds(int.Parse(commandSplit[2])),
            startColour: new Vector3(int.Parse(commandSplit[3]), int.Parse(commandSplit[4]), int.Parse(commandSplit[5])),
            endColour: new Vector3(int.Parse(commandSplit[6]), int.Parse(commandSplit[7]), int.Parse(commandSplit[8]))
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"C,{Easing},{StartTime},{EndTime},{StartColour.X},{StartColour.Y},{StartColour.Z},{EndColour.X},{EndColour.Y},{EndColour.Z}";
    }
}