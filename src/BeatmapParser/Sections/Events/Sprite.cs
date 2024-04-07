using System.Numerics;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Sprite : IEvent, ICommands
{
    /// <summary>
    /// 
    /// </summary>
    public EventType Type { get; init; } = EventType.Sprite;

    /// <summary>
    /// 
    /// </summary>
    public Layer Layer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Origin Origin { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<ICommand> Commands { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="origin"></param>
    /// <param name="filePath"></param>
    /// <param name="position"></param>
    /// <param name="commands"></param>
    protected Sprite(
        Layer layer,
        Origin origin,
        string filePath,
        Vector2 position,
        List<ICommand>? commands = null
        )
    {
        Layer = layer;
        Origin = origin;
        FilePath = filePath;
        Position = position;
        Commands = commands ?? ([]);
    }

    /// <summary>
    /// 
    /// </summary>
    protected Sprite()
    {
        Layer = Layer.Background;
        Origin = Origin.TopLeft;
        FilePath = string.Empty;
        Position = new Vector2();
        Commands = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"{EventType.Sprite},{(int)Layer},{(int)Origin},{FilePath},{Position.X},{Position.Y}";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <param name="commands"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Sprite Decode(string line, List<string> commands)
    {
        // Sprite,(layer),(origin),"(filepath)",(x),(y)

        var lineSplited = line.Trim().Split(',');

        var result = new Sprite
        (
            layer: (Layer)Enum.Parse(typeof(Layer), lineSplited[0]),
            origin: (Origin)Enum.Parse(typeof(Origin), lineSplited[1]),
            filePath: lineSplited[2],
            position: new Vector2(int.Parse(lineSplited[3]), int.Parse(lineSplited[4]))
        );

        List<ICommand> parsedCommands = [];
        foreach (var command in commands)
        {
            var commandSplit = command.Trim().Split(',');
            CommandTypes? identity = (CommandTypes)Enum.Parse(typeof(CommandTypes), commandSplit[0]); // TODO FIX

            ICommand commandDecoded = identity switch
            {
                CommandTypes.Fade => Fade.Decode(result, parsedCommands, command),
                CommandTypes.Move => Move.Decode(result, parsedCommands, command),
                CommandTypes.Scale => Scale.Decode(result, parsedCommands, command),
                CommandTypes.Rotate => Rotate.Decode(result, parsedCommands, command),
                CommandTypes.Colour => Colour.Decode(result, parsedCommands, command),
                CommandTypes.Parameter => Parameter.Decode(result, parsedCommands, command),
                _ => throw new Exception($"Unhandled command type \'{identity}\'"),
            };
            parsedCommands.Add(commandDecoded);
        }

        result.Commands = parsedCommands;
        return result;
    }
}