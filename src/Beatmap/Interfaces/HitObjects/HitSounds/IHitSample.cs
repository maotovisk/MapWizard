using Beatmap.Enums;

namespace Beatmap.HitObjects;
/// <summary>
///
/// </summary>
public interface IHitSample
{

    /// <summary>
    ///
    /// </summary>
    public SampleSet NormalSet { get; set; }

    /// <summary>
    ///
    /// </summary>
    public SampleSet AdditionSet { get; set; }

    /// <summary>
    ///
    /// </summary>
    public uint Index { get; set; }

    /// <summary>
    ///
    /// </summary>
    public uint Volume { get; set; }

    /// <summary>
    ///
    /// </summary>
    public FileInfo Filename { get; set; }
}