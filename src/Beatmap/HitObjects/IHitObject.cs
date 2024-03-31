using System.Numerics;
using Beatmap.Enums;

namespace Beatmap.HitObjects;

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
    public IHitSample HitSample { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public bool NewCombo { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    public uint ComboColour { get; set; }

}
