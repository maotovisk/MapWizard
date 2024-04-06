using System;
using System.Numerics;

namespace BeatmapParser;

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
        Looptype = LoopType.Forever;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public new string Encode()
    {
        return $"{EventType.Animation},{(int)Layer},{(int)Origin},{FilePath},{Position.X},{Position.Y},{FrameCount},{FrameDelay},{Looptype}";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public new static Animation Decode(string line, List<string> commands)
    {
        // Animation,(layer),(origin),"(filepath)",(x),(y),
        // (frameCount),(frameDelay),(looptype)

        var lineSplited = line.Trim().Split(',');

        var result = new Animation
        (
            layer: (Layer)Enum.Parse(typeof(Layer), lineSplited[0]),
            origin: (Origin)Enum.Parse(typeof(Origin), lineSplited[1]),
            filePath: lineSplited[2],
            position: new Vector2(int.Parse(lineSplited[3]), int.Parse(lineSplited[4])),
            frameCount: uint.Parse(lineSplited[5]),
            frameDelay: TimeSpan.FromMilliseconds(int.Parse(lineSplited[6])),
            looptype: (LoopType)Enum.Parse(typeof(LoopType), lineSplited[7].Skip(4).ToString() ?? throw new Exception("Invalid looptype"))
        );

        List<ICommand> parsedCommands = [];
        foreach (var command in commands)
        {
            var commandSplited = command.Trim().Split(',');
            CommandTypes? indentification = (CommandTypes)Enum.Parse(typeof(CommandTypes), commandSplited[0]); // TODO FIX

            ICommand commandDecoded = indentification switch
            {
                CommandTypes.Fade => Fade.Decode(result, parsedCommands, command),
                CommandTypes.Move => Move.Decode(result, parsedCommands, command),
                CommandTypes.Scale => Scale.Decode(result, parsedCommands, command),
                CommandTypes.Rotate => Rotate.Decode(result, parsedCommands, command),
                CommandTypes.Colour => Colour.Decode(result, parsedCommands, command),
                CommandTypes.Parameter => Parameter.Decode(result, parsedCommands, command),
                _ => throw new Exception($"Unhandled command type \'{indentification}\'"),
            };
            parsedCommands.Add(commandDecoded);
        }

        result.Commands = parsedCommands;
        return result;
    }
}