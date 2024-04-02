using System.Numerics;
using System.Runtime.InteropServices.Marshalling;

namespace BeatmapParser;

/// <summary>
/// Represents a spinner hit object in a beatmap.
/// </summary>
public class Spinner : HitObject, ISpinner
{
    /// <summary>
    /// Gets or sets the end time of the spinner.
    /// </summary>
    public TimeSpan End { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    /// <param name="coordinates">The coordinates of the spinner.</param>
    /// <param name="time">The time of the spinner.</param>
    /// <param name="type">The type of the spinner.</param>
    /// <param name="hitSounds">The list of hit sounds associated with the spinner.</param>
    /// <param name="newCombo">A value indicating whether the spinner starts a new combo.</param>
    /// <param name="comboColour">The color of the combo associated with the spinner.</param>
    /// <param name="end">The end time of the spinner.</param>
    public Spinner(Vector2 coordinates, TimeSpan time, HitObjectType type, (IHitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour, TimeSpan end)
    : base(coordinates, time, type, hitSounds, newCombo, comboColour)
    {
        End = end;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    public Spinner() : base()
    {
        Coordinates = new Vector2();
        Time = new TimeSpan();
        HitSounds = (new HitSample(), new List<HitSound>());
        End = new TimeSpan();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    public Spinner(HitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.Type, baseObject.HitSounds, baseObject.NewCombo, baseObject.ComboColour)
    {
        End = new TimeSpan();
    }
}