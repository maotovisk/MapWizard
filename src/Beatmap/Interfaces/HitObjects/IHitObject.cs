using System.Numerics;

namespace BeatmapParser;

/// <summary>
/// 
/// </summary>
public interface IHitObject
{
    /// <summary>
    /// 
    /// </summary>
    public Vector2 Coordinates { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Time { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public HitObjectType Type { get; set; }

    /// <summary>
    /// Hitsound and sampleset of the hit object.
    /// </summary>
    public (IHitSample SampleData, List<HitSound> Sounds) HitSounds { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public bool NewCombo { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public uint ComboColour { get; set; }

}
