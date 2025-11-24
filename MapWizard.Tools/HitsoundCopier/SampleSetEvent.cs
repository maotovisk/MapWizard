using BeatmapParser.Enums;

namespace MapWizard.Tools.HitSoundCopier;

/// <summary>
/// Class to store a sample set event.
/// </summary>
/// <remarks>
/// Create a new sample set event.
/// </remarks>
/// <param name="time"></param>
/// <param name="sample"></param>
/// <param name="index"></param>
/// <param name="volume"></param>
public class SampleSetEvent(double time, SampleSet sample, int index, double volume)
{
    /// <summary>
    /// Gets or sets the time of the sample set event.
    /// </summary>
    public double Time { get; set; } = time;
    /// <summary>
    /// Gets or sets the sample set of the sample set event.
    /// </summary>
    public SampleSet Sample { get; set; } = sample;
    /// <summary>
    /// Gets or sets the index of the sample set event.
    /// </summary>
    public int Index { get; set; } = index;
    /// <summary>
    /// Gets or sets the volume of the sample set event.
    /// </summary>
    public double Volume { get; set; } = volume;
}