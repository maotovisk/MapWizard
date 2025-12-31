using MapWizard.Tools.HitSoundCopier.Event;

namespace MapWizard.Tools.HitSoundCopier.Timeline;

/// <summary>
/// Class to store a sample set timeline.
/// </summary>
public class SampleSetTimeline
{
    /// <summary>
    /// List of sample set events.
    /// </summary>
    public List<SampleSetEvent> HitSamples;

    /// <summary>
    /// Create a new sample set timeline.
    /// </summary>
    public SampleSetTimeline()
    {
        HitSamples = [];
    }

    /// <summary>
    /// Get the sample set at a specific time.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="leniency"></param>
    /// <returns></returns>
    public SampleSetEvent? GetSampleAtExactTime(double time, int leniency = 2)
    {
        if (HitSamples.Count == 0)
            return null;

        return HitSamples.OrderBy(x => x.Time).LastOrDefault(x => Math.Abs(x.Time - time) <= leniency);
    }
    
    /// <summary>
    /// Get the current sample set information that affects the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="leniency"></param>
    /// <returns></returns>
    public SampleSetEvent? GetCurrentSampleAtTime(double time, int leniency = 2)
    {
        if (HitSamples.Count == 0)
            return null;
            
        return HitSamples.OrderBy(x => x.Time).LastOrDefault(x => x.Time <= time + leniency);
    }
}