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
    /// <param name="hitSounds"></param>
    /// <param name="hitSample"></param>
    /// <param name="newCombo"></param>
    /// <param name="comboColour"></param>
    public Circle(Vector2 coordinates, TimeSpan time, List<HitSound> hitSounds, IHitSample hitSample, bool newCombo, uint comboColour) : base(coordinates, time, hitSounds, hitSample, newCombo, comboColour)
    { }
    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    public Circle() : base()
    {
        Coordinates = new Vector2();
        Time = new TimeSpan();
        HitSounds = new List<HitSound>();
        HitSampleData = new HitSample();
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Initializes a new instance of the Circle class.
    /// </summary>
    /// <param name="baseObject"></param>
    public Circle(HitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.HitSounds, baseObject.HitSampleData, baseObject.NewCombo, baseObject.ComboColour)
    {
    }
}