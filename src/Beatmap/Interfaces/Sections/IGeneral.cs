namespace BeatmapParser;

/// <summary>
///  This is a osu file format v14 specification of the General section.
/// </summary>
public interface IGeneral
{
    /// <summary>
    /// Location of the audio file relative to the current folder
    /// </summary>
    string AudioFilename { get; set; }

    /// <summary>
    /// Milliseconds of silence before the audio starts playing
    /// </summary>
    int AudioLeadIn { get; set; }

    /// <summary>
    /// Time in milliseconds when the audio preview should start
    /// </summary>
    int PreviewTime { get; set; }

    /// <summary>
    /// Whether the countdown is enabled
    /// </summary>
    bool? Countdown { get; set; }

    /// <summary>
    /// Sample set that will be used if timing points do not override it (Normal, Soft, Drum)
    /// </summary>
    string SampleSet { get; set; }

    /// <summary>
    /// Multiplier for the threshold in time where hit objects placed close together stack (0–1)
    /// </summary>
    double StackLeniency { get; set; }

    /// <summary>
    /// Game mode (0 = osu!, 1 = Taiko, 2 = CtB, 3 = osu!mania)
    /// </summary>
    int Mode { get; set; }

    /// <summary>
    /// Whether or not breaks have a letterboxing effect
    /// </summary>
    bool? LetterboxInBreaks { get; set; }

    /// <summary>
    /// Whether or not the storyboard can use the user's skin images
    /// </summary>
    bool? UseSkinSprites { get; set; }

    /// <summary>
    /// Draw order of hit circle overlays compared to hit numbers (NoChange = use skin setting, Below = draw overlays under numbers, Above = draw overlays on top of numbers)
    /// </summary>
    string? OverlayPosition { get; set; }

    /// <summary>
    /// Preferred skin to use during gameplay
    /// </summary>
    string? SkinPreference { get; set; }

    /// <summary>
    /// Whether or not a warning about flashing colours should be shown at the beginning of the map
    /// </summary>
    bool? EpilepsyWarning { get; set; }

    /// <summary>
    /// Time in beats that the countdown starts before the first hit object
    /// </summary>
    double? CountdownOffset { get; set; }

    /// <summary>
    /// Whether or not the "N+1" style key layout is used for osu!mania
    /// </summary>
    bool? SpecialStyle { get; set; }

    /// <summary>
    /// Whether or not the storyboard allows widescreen viewing
    /// </summary>
    bool? WidescreenStoryboard { get; set; }

    /// <summary>
    /// Whether or not sound samples will change rate when playing with speed-changing mods
    /// </summary>
    bool? SamplesMatchPlaybackRate { get; set; }
}