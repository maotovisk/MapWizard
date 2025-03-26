using System.Numerics;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a hold object in a mania beatmap.
/// </summary>
public class ManiaHold : HitObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManiaHold"/> class.
    /// </summary>
    /// <param name="coordinates">The coordinates of the hold object.</param>
    /// <param name="time">The time of the hold object.</param>
    /// <param name="type">The type of the hold object.</param>
    /// <param name="hitSounds">The list of hit sounds for the hold object.</param>
    /// <param name="newCombo">A value indicating whether the hold object starts a new combo.</param>
    /// <param name="comboColour">The color of the combo for the hold object.</param>
    public ManiaHold(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour) : base(coordinates, time, type, hitSounds, newCombo, comboColour)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManiaHold"/> class.
    /// </summary>
    public ManiaHold() : base()
    {
        Coordinates = new Vector2();
        Time = new TimeSpan();
        HitSounds = (new HitSample(), new List<HitSound>());
        NewCombo = false;
        ComboOffset = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManiaHold"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    public ManiaHold(HitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.Type, baseObject.HitSounds, baseObject.NewCombo, baseObject.ComboOffset)
    {
    }
}