using System.Numerics;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a hit object in an osu! beatmap, including its essential properties
/// such as coordinates, timing, type, and sound configuration.
/// </summary>
public interface IHitObject : IEncodable
{
    /// <summary>
    /// Represents the positional coordinates of the hit object within the beatmap space,
    /// defined as a 2D vector with X and Y components.
    /// </summary>
    public Vector2 Coordinates { get; set; }

    /// <summary>
    /// Represents the time at which the hit object occurs in the beatmap timeline, represented as a TimeSpan value.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Represents the type of the hit object, specifying whether it is a circle, slider, spinner, or other supported types.
    /// </summary>
    public HitObjectType Type { get; }

    /// <summary>
    /// Represents the hitsound and sampleset of the hit object.
    /// </summary>
    public (HitSample SampleData, List<HitSound> Sounds) HitSounds { get; set; }

    /// <summary>
    /// Indicates whether the hit object starts a new combo in the beatmap.
    /// </summary>
    public bool NewCombo { get; set; }

    /// <summary>
    /// Represents the combo color attribute of the hit object, determining its visual appearance in gameplay.
    /// </summary>
    public uint ComboColour { get; set; }

}
