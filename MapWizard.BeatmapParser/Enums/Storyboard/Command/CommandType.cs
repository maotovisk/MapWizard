namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the different types of commands that can be used within a beatmap parser.
/// </summary>
public enum CommandType : int
{
    /// <summary>
    /// Represents a fade command in a beatmap, which controls the opacity
    /// of an object over a specified period of time.
    /// </summary>
    Fade,

    /// <summary>
    /// Represents a move command in a beatmap, which alters the position
    /// of an object over a specified period of time on the coordinate plane.
    /// </summary>
    Move,

    /// <summary>
    /// Represents a horizontal movement command in a beatmap, which adjusts
    /// the x-coordinate of an object over a specified period of time.
    /// </summary>
    MoveX,


    /// <summary>
    /// Represents a move command constrained to the Y-axis, used to adjust the vertical position
    /// of an object over a specified period of time in a beatmap.
    /// </summary>
    MoveY,

    /// <summary>
    /// Represents a scale command in a beatmap, which adjusts the size of an object
    /// over a specified period of time.
    /// </summary>
    Scale,

    /// <summary>
    /// Represents a vector scale command in a beatmap, which scales an object's dimensions
    /// independently along the x-axis and y-axis over a specified period of time.
    /// </summary>
    VectorScale,

    /// <summary>
    /// Represents a rotate command in a beatmap, which adjusts
    /// the rotation of an object over a specified period of time.
    /// </summary>
    Rotate,

    /// <summary>
    /// Represents a color command in a beatmap, which modifies the color properties
    /// of an object over a specified period of time.
    /// </summary>
    Color,

    /// <summary>
    /// Represents a colour command in a beatmap, which modifies the color of an object during gameplay.
    /// </summary>
    Colour,

    /// <summary>
    /// Represents a parameter command in a beatmap, typically used to modify
    /// non-transformational properties of an object, such as flipping or other settings.
    /// </summary>
    Parameter,

    /// <summary>
    /// Represents a loop command in a beatmap, which is used to repeat a specific sequence
    /// of events or commands multiple times.
    /// </summary>
    Loop,

    /// <summary>
    /// 
    /// </summary>
    Trigger
}