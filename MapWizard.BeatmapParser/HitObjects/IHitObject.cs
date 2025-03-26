using System.Numerics;

namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public interface IHitObject : IEncodable
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
    public HitObjectType Type { get; }

    /// <summary>
    /// Hitsound and sampleset of the hit object.
    /// </summary>
    public (HitSample SampleData, List<HitSound> Sounds) HitSounds { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public bool NewCombo { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public uint ComboOffset { get; set; }

}
