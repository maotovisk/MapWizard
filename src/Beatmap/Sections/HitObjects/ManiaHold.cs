using System.Numerics;

namespace BeatmapParser;

/// <summary>
/// Represents a hold object in a mania beatmap.
/// </summary>
public class ManiaHold : HitObject, IManiaHold
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManiaHold"/> class.
    /// </summary>
    /// <param name="coordinates">The coordinates of the hold object.</param>
    /// <param name="time">The time of the hold object.</param>
    /// <param name="hitSounds">The list of hit sounds for the hold object.</param>
    /// <param name="hitSample">The hit sample for the hold object.</param>
    /// <param name="newCombo">A value indicating whether the hold object starts a new combo.</param>
    /// <param name="comboColour">The color of the combo for the hold object.</param>
    public ManiaHold(Vector2 coordinates, TimeSpan time, List<HitSound> hitSounds, IHitSample hitSample, bool newCombo, uint comboColour) : base(coordinates, time, hitSounds, hitSample, newCombo, comboColour)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManiaHold"/> class.
    /// </summary>
    public ManiaHold() : base()
    {
        Coordinates = new Vector2();
        Time = new TimeSpan();
        HitSounds = new List<HitSound>();
        HitSampleData = new HitSample();
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManiaHold"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    public ManiaHold(HitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.HitSounds, baseObject.HitSampleData, baseObject.NewCombo, baseObject.ComboColour)
    {
    }
}