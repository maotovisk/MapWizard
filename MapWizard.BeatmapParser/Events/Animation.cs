using System.Numerics;
using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public class Animation : Sprite
{
    private double _frameDelayMilliseconds;

    /// <summary>
    /// Gets or sets the number of frames in the animation.
    /// Determines the total count of sequential images that compose the animation.
    /// </summary>
    public uint FrameCount { get; set; }

    /// <summary>
    /// Gets or sets the delay between consecutive frames in the animation.
    /// Specifies the time duration for which each frame is displayed.
    /// </summary>
    public TimeSpan FrameDelay
    {
        get => TimeSpan.FromMilliseconds(_frameDelayMilliseconds);
        set => _frameDelayMilliseconds = value.TotalMilliseconds;
    }

    /// <summary>
    /// Gets or sets the type of loop behavior for the animation.
    /// Determines whether the animation loops forever or plays only once.
    /// </summary>
    public LoopType Looptype { get; set; }

    /// <summary>
    /// Represents an animated sprite in a specified layer with a defined origin, file path, position,
    /// frame count, frame delay, and loop behavior.
    /// </summary>
    /// <remarks>
    /// The Animation class inherits from the Sprite class, and it adds properties and functionality
    /// necessary to handle frame-based animations for a sprite. This includes details like frame count,
    /// delay between frames, and how the animation should loop.
    /// </remarks>
    /// <param name="layer">The layer where the sprite resides (e.g., Background, Foreground).</param>
    /// <param name="origin">The origin point for the sprite's positioning.</param>
    /// <param name="filePath">The file path of the sprite's image or animation assets.</param>
    /// <param name="position">The initial position of the sprite.</param>
    /// <param name="frameCount">The total number of frames in the animation.</param>
    /// <param name="frameDelay">The delay between each frame in the animation.</param>
    /// <param name="looptype">Specifies the loop behavior of the animation (e.g., LoopOnce or LoopForever).</param>
    /// <param name="commands">
    /// A collection of ICommand instances that define additional behaviors or transformations
    /// for the animated sprite.
    /// </param>
    private Animation(
        Layer layer,
        Origin origin,
        string filePath,
        Vector2 position,
        uint frameCount,
        double frameDelayMilliseconds,
        LoopType looptype,
        List<ICommand>? commands = null
    ) : base(layer, origin, filePath, position, commands)
    {
        FrameCount = frameCount;
        _frameDelayMilliseconds = frameDelayMilliseconds;
        Looptype = looptype;
    }

    /// <summary>
    /// Represents an animated sprite within a specified layer, supporting frame-based animation,
    /// frame delay, and looping behavior.
    /// </summary>
    /// <remarks>
    /// The Animation class extends the Sprite class by providing additional properties and methods
    /// specific to animations, such as frame count, frame delay, and loop type. It also overrides
    /// encoding and decoding functionality to handle animation-specific details.
    /// </remarks>
    /// <param name="FrameCount">The total number of frames in the animation.</param>
    /// <param name="FrameDelay">The duration between each frame of the animation.</param>
    /// <param name="Looptype">Defines the looping behavior for the animation, such as looping indefinitely or once.</param>
    private Animation() : base()
    {
        FrameCount = 0;
        _frameDelayMilliseconds = 0;
        Looptype = LoopType.LoopForever;
    }

    /// <summary>
    /// Encodes the animation's properties and associated commands into a formatted string representation.
    /// </summary>
    /// <remarks>
    /// The output string is formatted according to the structure required for representing animations in the beatmap.
    /// It includes details such as the animation event type, layer, origin, file path, position, frame count,
    /// frame delay, loop type, and commands associated with the animation. Commands are serialized and appended
    /// to the string, with appropriate indentation for hierarchical representations.
    /// </remarks>
    /// <returns>A string that represents the serialized form of the animation, including its properties and commands.</returns>
    public new string Encode()
    {
        if (Commands.Count == 0) return $"{EventType.Animation},{(int)Layer},{(int)Origin},{FilePath},{Position.X.ToString(CultureInfo.InvariantCulture)},{Position.Y.ToString(CultureInfo.InvariantCulture)},{FrameCount},{_frameDelayMilliseconds.ToString(CultureInfo.InvariantCulture)},{Looptype}";
        StringBuilder builder = new();
        builder.AppendLine($"{EventType.Animation},{(int)Layer},{(int)Origin},{FilePath},{Position.X.ToString(CultureInfo.InvariantCulture)},{Position.Y.ToString(CultureInfo.InvariantCulture)},{FrameCount},{_frameDelayMilliseconds.ToString(CultureInfo.InvariantCulture)},{Looptype}");
        foreach (var command in Commands[..^1])
        {
            builder.AppendLine(command is IHasCommands ? string.Join(Environment.NewLine, command.Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + command.Encode());
        }

        builder.AppendLine(Commands.Last() is IHasCommands ? string.Join(Environment.NewLine, Commands.Last().Encode().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => " " + line)) : " " + Commands.Last().Encode());

        return builder.ToString();
    }

    /// <summary>
    /// Decodes a string representation of an animation into an <see cref="Animation"/> object by parsing
    /// relevant properties such as layer, origin, file path, position, frame count, frame delay, and loop type.
    /// </summary>
    /// <param name="line">The encoded string representation of the animation, structured as a comma-separated list
    /// containing animation properties.</param>
    /// <returns>An <see cref="Animation"/> object constructed from the decoded string.</returns>
    /// <exception cref="FormatException">Thrown when the provided string contains invalid or improperly formatted data.</exception>
    /// <exception cref="ArgumentException">Thrown when an invalid loop type is provided in the string.</exception>
    public new static Animation Decode(string line)
    {
        var lineSplit = line.Trim().Split(',');

        var result = new Animation
        (
            layer: (Layer)Enum.Parse(typeof(Layer), lineSplit[1]),
            origin: (Origin)Enum.Parse(typeof(Origin), lineSplit[2]),
            filePath: lineSplit[3],
            position: new Vector2(float.Parse(lineSplit[4], CultureInfo.InvariantCulture), float.Parse(lineSplit[5], CultureInfo.InvariantCulture)),
            frameCount: uint.Parse(lineSplit[6]),
            frameDelayMilliseconds: double.Parse(lineSplit[7], CultureInfo.InvariantCulture),
            looptype: lineSplit.Length > 8 ? (LoopType)Enum.Parse(typeof(LoopType), lineSplit[8] ?? throw new Exception("Invalid looptype")) : LoopType.LoopForever
        );

        return result;
    }
}
