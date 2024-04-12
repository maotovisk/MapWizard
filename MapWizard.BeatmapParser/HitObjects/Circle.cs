using System.Numerics;
namespace MapWizard.BeatmapParser;

/// <summary>
///
/// </summary>
public class Circle : HitObject
{

    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="time"></param>
    /// <param name="type"></param>
    /// <param name="hitSounds"></param>
    /// <param name="newCombo"></param>
    /// <param name="comboColour"></param>
    public Circle(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour) : base(coordinates, time, type, hitSounds, newCombo, comboColour)
    { }
    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    public Circle()
    {
    }

    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    /// <param name="baseObject"></param>
    private Circle(IHitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.Type, baseObject.HitSounds, baseObject.NewCombo, baseObject.ComboColour)
    {
    }

    /// <summary>
    /// Parses a hit object from a string.
    /// </summary>
    /// <param name="splitData"></param>
    /// <returns></returns>
    public new static Circle Decode(List<string> splitData)
    {
        return new Circle(HitObject.Decode(splitData));
    }
}