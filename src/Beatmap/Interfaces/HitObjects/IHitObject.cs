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
    public List<HitSound> HitSounds { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public IHitSample HitSampleData { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public bool NewCombo { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public uint ComboColour { get; set; }

}
