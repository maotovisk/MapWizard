using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a sprite event in a map, including its layer, origin point, file path, position,
/// and associated commands.
/// </summary>
public class Sprite : IEvent, IHasCommands, ILayeredEvent
{
    /// <summary>
    /// Gets the type of the event. See <see cref="EventType"/> for more information.
    /// </summary>
    public EventType Type { get; init; } = EventType.Sprite;

    /// <summary>
    /// Gets or sets the <see cref="Layer"/> in which the sprite event is positioned. Defaults to Background.
    /// </summary>
    public Layer Layer { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Origin"/> point of the sprite event. Defaults to TopLeft.
    /// </summary>
    public Origin Origin { get; set; }

    /// <summary>
    /// Gets or sets the file path of the sprite's image or animation.
    /// This property specifies the location of the visual asset used
    /// for rendering the sprite or animation in the map.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the 2D position of the sprite in the map.
    /// This property defines the coordinates where the sprite is placed on the canvas.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the list of commands associated with this sprite.
    /// Commands are transformations and actions applied to the sprite over time.
    /// See <see cref="Commands"/> for more information.
    /// </summary>
    public List<ICommand> Commands { get; set; }

    /// <summary>
    /// Represents a drawable Sprite object with position, origin, and layering information,
    /// as well as associated commands for animation or transformation effects.
    /// </summary>
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
    /// Represents a drawable sprite event in a map, including details such as the layer,
    /// origin point, file path, position, and any associated animation or transformation commands.
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
    /// Generates a serialized string representation of the Sprite object, including its type, layer, origin,
    /// file path, position, and associated commands, formatted according to the expected encoding standard.
    /// </summary>
    /// <returns>
    /// A string representing the encoded Sprite object. If no commands are present,
    /// the string includes only the basic sprite information. Otherwise, it includes the commands properly indented.
    /// </returns>
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
    /// Decodes a Sprite object from the provided string representation, parsing its layer, origin,
    /// file path, position, and other properties.
    /// </summary>
    /// <param name="line">The string representation of a Sprite to be parsed, formatted as
    /// "Sprite,(layer),(origin),"(filepath)",(x),(y)".</param>
    /// <returns>A <see cref="Sprite"/> object populated with parsed data from the input string.</returns>
    /// <exception cref="Exception">Thrown when the input string is invalid or parsing fails.</exception>
    public static Sprite Decode(string line)
    {
        try
        {
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