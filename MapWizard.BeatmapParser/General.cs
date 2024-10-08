using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the general section of a <see cref="Beatmap"/> .
/// </summary>
public class General
{
    /// <summary>
    /// Location of the audio file relative to the current folder
    /// </summary>
    public string AudioFilename { get; set; }

    /// <summary>
    /// Milliseconds of silence before the audio starts playing
    /// </summary>
    public int? AudioLeadIn { get; set; }

    /// <summary>
    /// Time in milliseconds when the audio preview should start
    /// </summary>
    public int? PreviewTime { get; set; }

    /// <summary>
    /// Whether the countdown is enabled
    /// </summary>
    public bool? Countdown { get; set; }

    /// <summary>
    /// Sample set that will be used if timing points do not override it (Normal, Soft, Drum)
    /// </summary>
    public string? SampleSet { get; set; }

    /// <summary>
    /// Multiplier for the threshold in time where hit objects placed close together stack (0–1)
    /// </summary>
    public double? StackLeniency { get; set; }

    /// <summary>
    /// Game mode (0 = osu!, 1 = Taiko, 2 = CtB, 3 = osu!mania)
    /// </summary>
    public int? Mode { get; set; }

    /// <summary>
    /// Whether or not breaks have a letterbox effect
    /// </summary>
    public bool? LetterboxInBreaks { get; set; }

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
    private General(
        string audioFilename,
        int? audioLeadIn,
        int? previewTime,
        bool? countdown,
        string? sampleSet,
        double? stackLeniency,
        int? mode,
        bool? letterboxInBreaks,
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
        SampleSet = "Normal";
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
                var splitLine = line.Split(':', 2);

                if (splitLine.Length < 1)
                {
                    throw new Exception($"Invalid General section field: {line}");
                }

                general.Add(splitLine[0].Trim(), splitLine.Length != 1 ? splitLine[1].Trim() : string.Empty);
            });

            if (Helper.IsWithinPropertyQuantity<General>(general.Count))
            {
                throw new Exception("Invalid General section length. Missing properties: " + string.Join(", ", Helper.GetMissingPropertiesNames<General>(general.Keys)) + ".");
            }

            return new General(
                audioFilename: general["AudioFilename"],
                audioLeadIn: general.TryGetValue("AudioLeadIn", out var audioLeadIn) ? int.Parse(audioLeadIn) : 0,
                previewTime: general.TryGetValue("PreviewTime", out var previewTime) ? int.Parse(previewTime) : 0,
                countdown: general.TryGetValue("Countdown", out var countdown) ? int.Parse(countdown) == 1 : null,
                sampleSet: general.GetValueOrDefault("SampleSet", "Normal"),
                stackLeniency: general.TryGetValue("StackLeniency", out var stackLeniency) ? double.Parse(stackLeniency, CultureInfo.InvariantCulture) : 7,
                mode: general.TryGetValue("Mode", out var mode) ? int.Parse(mode) : 0,
                letterboxInBreaks: general.TryGetValue("LetterboxInBreaks", out var letterboxInBreaks) ? int.Parse(letterboxInBreaks) == 1 : null,
                useSkinSprites: general.TryGetValue("UseSkinSprites", out var skinSprites) ? int.Parse(skinSprites) == 1 : null,
                overlayPosition: general.GetValueOrDefault("OverlayPosition"),
                skinPreference: general.GetValueOrDefault("SkinPreference"),
                epilepsyWarning: general.TryGetValue("EpilepsyWarning", out var epilepsyWarn) ? int.Parse(epilepsyWarn) == 1 : null,
                countdownOffset: general.TryGetValue("CountdownOffset", out var countdownOff) ? double.Parse(countdownOff, CultureInfo.InvariantCulture) : null,
                specialStyle: general.TryGetValue("SpecialStyle", out var specStyle) ? int.Parse(specStyle) == 1 : null,
                widescreenStoryboard: general.TryGetValue("WidescreenStoryboard", out var wideStoryboard) ? int.Parse(wideStoryboard) == 1 : null,
                samplesMatchPlaybackRate: general.TryGetValue("SamplesMatchPlaybackRate", out var samplesMatch) ? int.Parse(samplesMatch) == 1 : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing General section:\n{ex}.");
        }
    }

    /// <summary>
    /// Encodes the <see cref="General"/> class into a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        foreach (var prop in typeof(General).GetProperties())
        {
            if (prop.GetValue(this) is null) continue;

            if (prop.GetValue(this) is bool boolValue)
            {
                builder.AppendLine($"{prop.Name}: {(boolValue ? 1 : 0)}");
                continue;
            }

            if (prop.GetValue(this) is double doubleValue)
            {
                builder.AppendLine($"{prop.Name}: {doubleValue.ToString(CultureInfo.InvariantCulture)}");
                continue;
            }

            builder.AppendLine($"{prop.Name}: {prop.GetValue(this)}");
        }

        return builder.ToString();
    }
}