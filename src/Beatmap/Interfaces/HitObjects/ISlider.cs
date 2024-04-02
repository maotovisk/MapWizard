using System.Numerics;

namespace BeatmapParser;

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
    public double Length { get; set; }


    /// <summary>
    /// Hit sound of the slider track.
    /// </summary>
    public (IHitSample SampleData, List<HitSound> Sounds) HeadSounds { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<(IHitSample SampleData, List<HitSound> Sounds)>? RepeatSounds { get; set; }


    /// <summary>
    /// Hit sound of the slider track.
    /// </summary>
    public (IHitSample SampleData, List<HitSound> Sounds) TailSounds { get; set; }


}