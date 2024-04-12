using System.Numerics;
using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Animation : Sprite
{
    /// <summary>
    /// 
    /// </summary>
    public uint FrameCount { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan FrameDelay { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public LoopType Looptype { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="origin"></param>
    /// <param name="filePath"></param>
    /// <param name="position"></param>
    /// <param name="frameCount"></param>
    /// <param name="frameDelay"></param>
    /// <param name="looptype"></param>
    /// <param name="commands"></param>
    private Animation(
        Layer layer,
        Origin origin,
        string filePath,
        Vector2 position,
        uint frameCount,
        TimeSpan frameDelay,
        LoopType looptype,
        List<ICommand>? commands = null
        ) : base(layer, origin, filePath, position, commands)
    {
        FrameCount = frameCount;
        FrameDelay = frameDelay;
        Looptype = looptype;
    }

    /// <summary>
    /// 
    /// </summary>
    private Animation() : base()
    {
        FrameCount = 0;
        FrameDelay = TimeSpan.FromMilliseconds(0);
        Looptype = LoopType.LoopForever;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public new string Encode()
    {
        if (Commands.Count == 0) return $"{EventType.Animation},{(int)Layer},{(int)Origin},{FilePath},{Position.X.ToString(CultureInfo.InvariantCulture)},{Position.Y.ToString(CultureInfo.InvariantCulture)},{FrameCount},{FrameDelay},{Looptype}";
        StringBuilder builder = new();
        builder.AppendLine($"{EventType.Animation},{(int)Layer},{(int)Origin},{FilePath},{Position.X.ToString(CultureInfo.InvariantCulture)},{Position.Y.ToString(CultureInfo.InvariantCulture)},{FrameCount},{FrameDelay},{Looptype}");
        foreach (var command in Commands[..^1])
        {
            builder.AppendLine(command is ICommands ? string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + command.Encode());
        }

        builder.AppendLine(Commands.Last() is ICommands ? string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + Commands.Last().Encode());

        return builder.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public new static Animation Decode(string line)
    {
        // Animation,(layer),(origin),"(filepath)",(x),(y),
        // (frameCount),(frameDelay),(looptype)

        var lineSplit = line.Trim().Split(',');

        var result = new Animation
        (
            layer: (Layer)Enum.Parse(typeof(Layer), lineSplit[1]),
            origin: (Origin)Enum.Parse(typeof(Origin), lineSplit[2]),
            filePath: lineSplit[3],
            position: new Vector2(float.Parse(lineSplit[4], CultureInfo.InvariantCulture), float.Parse(lineSplit[5], CultureInfo.InvariantCulture)),
            frameCount: uint.Parse(lineSplit[6]),
            frameDelay: TimeSpan.FromMilliseconds(double.Parse(lineSplit[7], CultureInfo.InvariantCulture)),
            looptype: lineSplit.Length > 8 ? (LoopType)Enum.Parse(typeof(LoopType), lineSplit[8] ?? throw new Exception("Invalid looptype")) : LoopType.LoopForever
        );

        return result;
    }
}