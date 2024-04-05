using BeatmapParser;

namespace Tools.HitsoundCopier;

/// <summary>
/// Represents a timeline of hitsounds.
/// </summary>
public class SoundTimeline()
{
    /// <summary>
    /// Gets or sets the list of sound events in the timeline.
    /// </summary>
    public List<SoundEvent> SoundEvents { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the SoundTimeline class.
    /// </summary>
    /// <param name="soundEvents">The list of sound events in the timeline.</param>
    public SoundTimeline(List<SoundEvent> soundEvents)
        : this()
    {
        SoundEvents = soundEvents;
    }

    /// <summary>
    /// Gets the sound event at a specific time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public SoundEvent GetSoundAtTime(TimeSpan time)
    {
        return SoundEvents.FirstOrDefault(x => x.Time == time, new SoundEvent());
    }


}