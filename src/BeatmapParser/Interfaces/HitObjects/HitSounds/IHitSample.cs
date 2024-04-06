using System.IO.Enumeration;

namespace MapWizard.BeatmapParser;
/// <summary>
/// Represents a hit sample of a hit object.
/// </summary>
public interface IHitSample : IEncodable
{

    /// <summary>
    /// Sample set of the hit sample.
    /// </summary>
    public SampleSet NormalSet { get; set; }

    /// <summary>
    /// Sample set of the addition.
    /// </summary>
    public SampleSet AdditionSet { get; set; }

    /// <summary>
    /// Index of the hit sample.
    /// </summary>
    public uint? Index { get; set; }

    /// <summary>
    /// Volume of the hit sample.
    /// </summary>
    public uint? Volume { get; set; }

    /// <summary>
    /// Custom file name of the hit sample.
    /// </summary>
    public string? FileName { get; set; }
}