using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Sprite : IEvent, IHasCommands, ILayeredEvent
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
        if (Commands.Count == 0) return $"{EventType.Sprite},{(int)Layer},{(int)Origin},{FilePath},{Position.X.ToString(CultureInfo.InvariantCulture)},{Position.Y.ToString(CultureInfo.InvariantCulture)}";

        StringBuilder builder = new();
        builder.AppendLine($"{EventType.Sprite},{Layer},{Origin},{FilePath},{Position.X.ToString(CultureInfo.InvariantCulture)},{Position.Y.ToString(CultureInfo.InvariantCulture)}");
        foreach (var command in Commands[..^1])
        {
            builder.AppendLine(command is IHasCommands ? string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + command.Encode());
        }

        builder.AppendLine(Commands.Last() is IHasCommands ? string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + Commands.Last().Encode());



        return builder.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <param name="commands"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Sprite Decode(string line)
    {
        try
        {
            // Sprite,(layer),(origin),"(filepath)",(x),(y)

            var lineSplited = line.Trim().Split(',');

            var result = new Sprite
            (
                layer: (Layer)Enum.Parse(typeof(Layer), lineSplited[1]),
                origin: (Origin)Enum.Parse(typeof(Origin), lineSplited[2]),
                filePath: lineSplited[3],
                position: new Vector2(float.Parse(lineSplited[4], CultureInfo.InvariantCulture), float.Parse(lineSplited[5], CultureInfo.InvariantCulture))
            );
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing Sprite-> {line}: {ex}");
        }
    }
}