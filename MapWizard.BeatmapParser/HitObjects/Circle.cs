using System.Numerics;
namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a circle hit object in a beatmap.
/// </summary>
public class Circle : HitObject
{
    /// <summary>
    /// Initializes a new instance of the Circle class with specified parameters.
    /// </summary>
    /// <param name="coordinates">The coordinates of the circle in the beatmap.</param>
    /// <param name="time">The time at which the circle should be hit.</param>
    /// <param name="type">The type of the hit object, representing a circle.</param>
    /// <param name="hitSounds">The sounds to be played when the circle is hit.</param>
    /// <param name="newCombo">Indicates whether the circle starts a new combo.</param>
    /// <param name="comboOffset">The combo color offset used for this circle.</param>
    public Circle(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds,
        bool newCombo, uint comboOffset) : base(coordinates, time, type, hitSounds, newCombo, comboOffset)
    {
    }

    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    public Circle()
    {
    }

    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    /// <param name="baseObject">The base hit object to copy properties from.</param>
    private Circle(HitObject baseObject) : base(baseObject)
    {
    }

    /// <summary>
    /// Parses a hit object from a string.
    /// </summary>
    /// <param name="splitData">The string containing the hit object data, split by commas.</param>
    /// <returns></returns>
    public new static Circle Decode(List<string> splitData)
    {
        var baseObject = HitObject.Decode(splitData);
        return new Circle(baseObject);
    }
}
