using MapWizard.BeatmapParser;

namespace MapWizard.Tools.HitSoundCopier;

/// <summary>
/// Represents a timeline of hitsounds.
/// </summary>
public class SoundEvent()
{
    /// <summary>
    /// Gets or sets the time of the sound event.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Gets or sets the list of hit sounds for the sound event.
    /// </summary>
    public List<HitSound> HitSounds { get; set; } = [];

    /// <summary>
    /// Gets or sets the normal sample set for the sound event.
    /// </summary>
    public SampleSet NormalSample { get; set; }

    /// <summary>
    /// Gets or sets the addition sample set for the sound event.
    /// </summary>
    public SampleSet AdditionSample { get; set; }

    /// <summary>
    /// Initializes a new instance of the SoundEvent class.
    /// </summary>
    /// <param name="time">The time of the sound event.</param>
    /// <param name="hitSounds">The list of hit sounds for the sound event.</param>
    /// <param name="normalSample">The normal sample set for the sound event.</param>
    /// <param name="additionSample">The addition sample set for the sound event.</param>
    public SoundEvent(TimeSpan time, List<HitSound> hitSounds, SampleSet normalSample, SampleSet additionSample)
        : this()
    {
        Time = time;
        HitSounds = hitSounds;
        NormalSample = normalSample;
        AdditionSample = additionSample;
    }

}