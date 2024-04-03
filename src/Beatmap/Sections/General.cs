using System.Globalization;

namespace BeatmapParser.Sections;

/// <summary>
/// Represents the general section of a <see cref="Beatmap"/> .
/// </summary>
public class General : IGeneral
{
    /// <summary>
    /// Location of the audio file relative to the current folder
    /// </summary>
    public string AudioFilename { get; set; }

    /// <summary>
    /// Milliseconds of silence before the audio starts playing
    /// </summary>
    public int AudioLeadIn { get; set; }

    /// <summary>
    /// Time in milliseconds when the audio preview should start
    /// </summary>
    public int PreviewTime { get; set; }

    /// <summary>
    /// Whether the countdown is enabled
    /// </summary>
    public bool Countdown { get; set; }

    /// <summary>
    /// Sample set that will be used if timing points do not override it (Normal, Soft, Drum)
    /// </summary>
    public string SampleSet { get; set; }

    /// <summary>
    /// Multiplier for the threshold in time where hit objects placed close together stack (0–1)
    /// </summary>
    public double StackLeniency { get; set; }

    /// <summary>
    /// Game mode (0 = osu!, 1 = Taiko, 2 = CtB, 3 = osu!mania)
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Whether or not breaks have a letterboxing effect
    /// </summary>
    public bool LetterboxInBreaks { get; set; }

    /// <summary>
    /// Whether or not the storyboard can use the user's skin images
    /// </summary>
    public bool? UseSkinSprites { get; set; }

    /// <summary>
    /// Draw order of hit circle overlays compared to hit numbers (NoChange = use skin setting, Below = draw overlays under numbers, Above = draw overlays on top of numbers)
    /// </summary>
    public string? OverlayPosition { get; set; }

    /// <summary>
    /// Preferred skin to use during gameplay
    /// </summary>
    public string? SkinPreference { get; set; }

    /// <summary>
    /// Whether or not a warning about flashing colours should be shown at the beginning of the map
    /// </summary>
    public bool? EpilepsyWarning { get; set; }

    /// <summary>
    /// Time in beats that the countdown starts before the first hit object
    /// </summary>
    public double? CountdownOffset { get; set; }

    /// <summary>
    /// Whether or not the "N+1" style key layout is used for osu!mania
    /// </summary>
    public bool? SpecialStyle { get; set; }

    /// <summary>
    /// Whether or not the storyboard allows widescreen viewing
    /// </summary>
    public bool? WidescreenStoryboard { get; set; }

    /// <summary>
    /// Whether or not sound samples will change rate when playing with speed-changing mods
    /// </summary>
    public bool? SamplesMatchPlaybackRate { get; set; }

    /// <summary>
    /// Creates a new <see cref="General"/> section of a <see cref="Beatmap"/> with the provided values.
    /// </summary>
    /// <param name="audioFilename"></param>
    /// <param name="audioLeadIn"></param>
    /// <param name="previewTime"></param>
    /// <param name="countdown"></param>
    /// <param name="sampleSet"></param>
    /// <param name="stackLeniency"></param>
    /// <param name="mode"></param>
    /// <param name="letterboxInBreaks"></param>
    /// <param name="useSkinSprites"></param>
    /// <param name="overlayPosition"></param>
    /// <param name="skinPreference"></param>
    /// <param name="epilepsyWarning"></param>
    /// <param name="countdownOffset"></param>
    /// <param name="specialStyle"></param>
    /// <param name="widescreenStoryboard"></param>
    /// <param name="samplesMatchPlaybackRate"></param>
    public General(
        string audioFilename,
        int audioLeadIn,
        int previewTime,
        bool countdown,
        string sampleSet,
        double stackLeniency,
        int mode,
        bool letterboxInBreaks,
        bool? useSkinSprites,
        string? overlayPosition,
        string? skinPreference,
        bool? epilepsyWarning,
        double? countdownOffset,
        bool? specialStyle,
        bool? widescreenStoryboard,
        bool? samplesMatchPlaybackRate
    )
    {
        AudioFilename = audioFilename;
        AudioLeadIn = audioLeadIn;
        PreviewTime = previewTime;
        Countdown = countdown;
        SampleSet = sampleSet;
        StackLeniency = stackLeniency;
        Mode = mode;
        LetterboxInBreaks = letterboxInBreaks;
        UseSkinSprites = useSkinSprites;
        OverlayPosition = overlayPosition;
        SkinPreference = skinPreference;
        EpilepsyWarning = epilepsyWarning;
        CountdownOffset = countdownOffset;
        SpecialStyle = specialStyle;
        WidescreenStoryboard = widescreenStoryboard;
        SamplesMatchPlaybackRate = samplesMatchPlaybackRate;
    }

    /// <summary>
    /// Creates a new <see cref="General"/> section of a <see cref="Beatmap"/> with default values.
    /// </summary>
    public General()
    {
        AudioFilename = string.Empty;
        AudioLeadIn = 0;
        PreviewTime = 0;
        Countdown = false;
        SampleSet = string.Empty;
        StackLeniency = 0;
        Mode = 0;
        LetterboxInBreaks = false;
        UseSkinSprites = false;
        OverlayPosition = string.Empty;
        SkinPreference = string.Empty;
        EpilepsyWarning = false;
        CountdownOffset = 0;
        SpecialStyle = false;
        SamplesMatchPlaybackRate = false;
    }

    /// <summary>
    /// Parses a list of General lines into a new <see cref="General"/> class.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static General Decode(List<string> section)
    {
        Dictionary<string, string> general = [];

        try
        {
            section.ForEach(line =>
            {
                string[] splittedLine = line.Split(':');

                if (splittedLine.Length < 2)
                {
                    throw new Exception($"Invalid General section field: {line}");
                }

                if (general.ContainsKey(splittedLine[0].Trim()))
                {
                    throw new Exception("Adding same propriety multiple times.");
                }

                general.Add(splittedLine[0].Trim(), string.Join(":", splittedLine.Skip(1)).Trim());
            });

            if (Helper.IsWithinProperitesQuantitity<IGeneral>(general.Count))
            {
                throw new Exception("Invalid General section length. Missing properties: " + string.Join(", ", Helper.GetMissingPropertiesNames<IGeneral>(general.Keys)) + ".");
            }

            return new General(
                audioFilename: general["AudioFilename"],
                audioLeadIn: int.Parse(general["AudioLeadIn"]),
                previewTime: int.Parse(general["PreviewTime"]),
                countdown: int.Parse(general["Countdown"]) == 1,
                sampleSet: general["SampleSet"],
                stackLeniency: double.Parse(general["StackLeniency"], CultureInfo.InvariantCulture),
                mode: int.Parse(general["Mode"]),
                letterboxInBreaks: int.Parse(general["LetterboxInBreaks"]) == 1,
                useSkinSprites: general.ContainsKey("UseSkinSprites") ? int.Parse(general["UseSkinSprites"]) == 1 : null,
                overlayPosition: general.ContainsKey("OverlayPosition") ? general["OverlayPosition"] : null,
                skinPreference: general.ContainsKey("SkinPreference") ? general["SkinPreference"] : null,
                epilepsyWarning: general.ContainsKey("EpilepsyWarning") ? int.Parse(general["EpilepsyWarning"]) == 1 : null,
                countdownOffset: general.ContainsKey("CountdownOffset") ? double.Parse(general["CountdownOffset"], CultureInfo.InvariantCulture) : null,
                specialStyle: general.ContainsKey("SpecialStyle") ? int.Parse(general["SpecialStyle"]) == 1 : null,
                widescreenStoryboard: general.ContainsKey("WidescreenStoryboard") ? int.Parse(general["WidescreenStoryboard"]) == 1 : null,
                samplesMatchPlaybackRate: general.ContainsKey("SamplesMatchPlaybackRate") ? int.Parse(general["SamplesMatchPlaybackRate"]) == 1 : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing General section:\n{ex}.");
        }
    }
}