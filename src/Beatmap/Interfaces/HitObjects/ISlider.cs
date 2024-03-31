using System.Numerics;
using Beatmap.Enums;

namespace Beatmap.HitObjects;

/// <summary>
///
/// </summary>
public interface ISlider : IHitObject
{
    /// <summary>
    ///
    /// </summary>
    public CurveType CurveType { get; set; }

    /// <summary>
    ///
    /// </summary>
    public List<Vector2> CurvePoints { get; set; }

    /// <summary>
    ///
    /// </summary>
    public uint Repeats { get; set; }

    /// <summary>
    ///
    /// </summary>
    public uint Lenght { get; set; }

    /// <summary>
    ///
    /// </summary>
    public HitSound EdgeHitSound { get; set; }

    /// <summary>
    ///
    /// </summary>
    public SampleSet EdgeSampleSets { get; set; }
}