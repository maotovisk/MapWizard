using System.Numerics;
namespace BeatmapParser;

/// <summary>
///
/// </summary>
public class Circle : HitObject, ICircle
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
    public Circle(Vector2 coordinates, TimeSpan time, HitObjectType type, (IHitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour) : base(coordinates, time, type, hitSounds, newCombo, comboColour)
    { }
    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    public Circle() : base()
    {
        Coordinates = new Vector2();
        Time = new TimeSpan();
        HitSounds = (new HitSample(), new List<HitSound>());
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    /// <param name="baseObject"></param>
    public Circle(HitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.Type, baseObject.HitSounds, baseObject.NewCombo, baseObject.ComboColour)
    {
    }
}