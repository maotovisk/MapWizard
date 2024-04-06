using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using BeatmapParser.Sections;
using Tools.HitsoundCopier;

namespace BeatmapParser;

/// <summary>
/// Represents an osu! beatmap.
/// </summary>
public class Beatmap : IBeatmap
{
    /// <summary>
    /// The format version of the beatmap.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The metadata section of the beatmap.
    /// </summary>
    public IMetadata Metadata { get; set; }
    /// <summary>
    /// The general section of the beatmap.
    /// </summary>
    public IGeneral General { get; set; }
    /// <summary>
    /// The editor section of the beatmap.
    /// </summary>
    public IEditor? Editor { get; set; }
    /// <summary>
    /// The difficulty section of the beatmap.
    /// </summary>
    public IDifficulty Difficulty { get; set; }
    /// <summary>
    /// The colours section of the beatmap.
    /// </summary>
    public IColours? Colours { get; set; }
    /// <summary>
    /// The events section of the beatmap.
    /// </summary>
    public IEvents Events { get; set; }
    /// <summary>
    /// The timing points section of the beatmap.
    /// </summary>
    public ITimingPoints? TimingPoints { get; set; }
    /// <summary>
    /// The hit objects section of the beatmap.
    /// </summary>
    public IHitObjects HitObjects { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Beatmap"/> class.
    /// </summary>
    public Beatmap()
    {
        Metadata = new Metadata();
        General = new General();
        Difficulty = new Difficulty();
        Events = new Events();
        HitObjects = new HitObjects();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Beatmap"/> class with the specified parameters.
    /// </summary>
    /// <param name="version"></param>
    /// <param name="metadata"></param>
    /// <param name="general"></param>
    /// <param name="editor"></param>
    /// <param name="difficulty"></param>
    /// <param name="colours"></param>
    /// <param name="events"></param>
    /// <param name="timingPoints"></param>
    /// <param name="hitObjects"></param>
    public Beatmap(int version, IMetadata metadata, IGeneral general, IEditor? editor, IDifficulty difficulty, IColours? colours, IEvents events, ITimingPoints? timingPoints, IHitObjects hitObjects)
    {
        Version = version;
        Metadata = metadata;
        General = general;
        Editor = editor;
        Difficulty = difficulty;
        Colours = colours;
        Events = events;
        TimingPoints = timingPoints;
        HitObjects = hitObjects;
    }

    /// <summary>
    /// Decodes a dictionary of sections into a <see cref="Beatmap"/>.
    /// For now only version 14 is supported.
    /// </summary>
    /// <param name="beatmapString"></param>
    /// <returns></returns>
    public static Beatmap Decode(string beatmapString)
    {
        var lines = beatmapString.Split(["\r\n", "\n"], StringSplitOptions.None)
                           .Where(line => !string.IsNullOrEmpty(line.Trim()) || !string.IsNullOrWhiteSpace(line.Trim()))
                           .ToList();

        if (lines.Count == 0) throw new Exception("Beatmap is empty.");

        var sections = new Dictionary<string, List<string>>();
        var currentSection = string.Empty;
        int formatVersion = -1;

        if (!lines[0].Contains("file format")) throw new Exception("Invalid file format.");

        formatVersion = int.Parse(lines[0].Split("v")[1].Trim());

        foreach (var line in lines[1..])
        {
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line.Trim('[', ']');
                sections[currentSection] = [];
            }
            else
            {
                sections[currentSection].Add(currentSection == "Events" ? line : line.Trim());
            }
        }

        if (Helper.IsWithinProperitesQuantitity<IBeatmap>(sections.Count)) throw new Exception($"Invalid number of sections. Expected {typeof(IBeatmap).GetProperties().Length} but got {sections.Count}.");

        General general = Sections.General.Decode(sections[$"{SectionTypes.General}"]);
        var editor = sections.ContainsKey($"{SectionTypes.Editor}") ? Sections.Editor.Decode(sections[$"{SectionTypes.Editor}"]) : null;
        var metadata = Sections.Metadata.Decode(sections[$"{SectionTypes.Metadata}"]);
        var difficulty = Sections.Difficulty.Decode(sections[$"{SectionTypes.Difficulty}"]);
        var colours = sections.ContainsKey($"{SectionTypes.Colours}") ? Sections.Colours.Decode(sections[$"{SectionTypes.Colours}"]) : null;
        var events = Sections.Events.Decode(sections[$"{SectionTypes.Events}"]);
        var timingPoints = sections.ContainsKey($"{SectionTypes.TimingPoints}") ? Sections.TimingPoints.Decode(sections[$"{SectionTypes.TimingPoints}"]) : null;
        var hitObjects = Sections.HitObjects.Decode(sections[$"{SectionTypes.HitObjects}"], timingPoints ?? new Sections.TimingPoints(), difficulty);

        return new Beatmap(
            formatVersion, metadata, general, editor, difficulty, colours, events, timingPoints, hitObjects
        );
    }

    /// <summary>
    /// Converts a .osu file into a <see cref="Beatmap"/> object.
    /// </summary>
    /// <param name="path">Path of tthe beatmap</param>
    /// <returns>Dictionary of sections</returns>
    /// <exception cref="Exception"></exception>
    public static Beatmap Decode(FileInfo path) => Decode(File.ReadAllText(path.FullName));


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        builder.AppendLine($"osu file format v14"); // we are only supporting v14
        builder.AppendLine();

        builder.AppendLine($"[{SectionTypes.General}]");
        builder.AppendLine(General.Encode());

        if (Editor != null)
        {
            builder.AppendLine($"[{SectionTypes.Editor}]");
            builder.AppendLine(Editor.Encode());
        }

        builder.AppendLine($"[{SectionTypes.Metadata}]");
        builder.AppendLine(Metadata.Encode());

        builder.AppendLine($"[{SectionTypes.Difficulty}]");
        builder.AppendLine(Difficulty.Encode());

        builder.AppendLine($"[{SectionTypes.Events}]");
        if (Events.EventList.Count > 0)
            builder.Append(Events.Encode());
        builder.AppendLine();

        if (TimingPoints != null)
        {
            builder.AppendLine($"[{SectionTypes.TimingPoints}]");
            if (TimingPoints.TimingPointList.Count > 0)
                builder.AppendLine(TimingPoints.Encode());
            builder.AppendLine();
        }

        if (Colours != null)
        {
            builder.AppendLine($"[{SectionTypes.Colours}]");
            builder.AppendLine(Colours.Encode());
        }

        builder.AppendLine($"[{SectionTypes.HitObjects}]");
        builder.AppendLine(HitObjects.Encode());
        builder.AppendLine();

        return builder.ToString();
    }

    /// <summary>
    /// Gets the uninherited timing point at the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public UninheritedTimingPoint? GetUninheritedTimingPointAt(double time)
    {
        if (TimingPoints == null) return null;

        if (TimingPoints is TimingPoints section)
        {
            return section.GetUninheritedTimingPointAt(time);
        }
        return null;
    }

    /// <summary>
    /// Gets the inherited timing point at the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public InheritedTimingPoint? GetInheritedTimingPointAt(double time)
    {
        if (TimingPoints == null) return null;

        if (TimingPoints is TimingPoints section)
        {
            return section.GetInheritedTimingPointAt(time);
        }
        return null;
    }

    /// <summary>
    /// Returns the volume at the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public uint GetVolumeAt(double time)
    {
        if (TimingPoints == null) return 100;

        if (TimingPoints is TimingPoints section)
        {
            return section.GetVolumeAt(time);
        }
        return 100;
    }

    /// <summary>
    /// Returns the BPM at the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public double GetBpmAt(double time)
    {
        if (TimingPoints == null) return 120;

        if (TimingPoints is TimingPoints section)
        {
            return section.GetBpmAt(time);
        }
        return 120;
    }

    /// <summary>
    /// Gets the hit object at a specific time.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="leniency"></param>
    /// <returns></returns>
    public IHitObject? GetHitObjectAt(double time, int leniency = 2)
    {
        return ((Sections.HitObjects)HitObjects).GetHitObjectAt(time, leniency);
    }

    /// <summary>
    /// Applies a hitsound to hitobjects at the specified time.
    /// </summary>
    /// <param name="hitSoundTimeline"></param>
    /// <param name="leniency"></param>
    public void ApplyNonDraggableHitsounds(SoundTimeline hitSoundTimeline, int leniency = 2)
    {
        if (HitObjects == null) return;
        if (TimingPoints == null) return;

        if (HitObjects is Sections.HitObjects section)
        {
            foreach (var hitObject in HitObjects.Objects)
            {
                if (hitObject is Circle circle)
                {
                    var currentSound = hitSoundTimeline.GetSoundAtTime(circle.Time);

                    if (currentSound != null && (Math.Abs(circle.Time.TotalMilliseconds - currentSound.Time.TotalMilliseconds) <= leniency))
                    {
                        circle.HitSounds = (new HitSample(
                            normalSet: currentSound.NormalSample,
                            additionSet: currentSound.AdditionSample,
                            0,
                            0,
                            circle.HitSounds.SampleData.FileName
                        ), currentSound.HitSounds);
                    }
                }
                else if (hitObject is Slider slider)
                {
                    var currentHeadSound = hitSoundTimeline.GetSoundAtTime(slider.Time);

                    if (currentHeadSound != null && (Math.Abs(slider.Time.TotalMilliseconds - currentHeadSound.Time.TotalMilliseconds) <= leniency))
                    {
                        slider.HeadSounds = (new HitSample(
                            normalSet: currentHeadSound.NormalSample,
                            additionSet: currentHeadSound.AdditionSample,
                            0,
                            0,
                            slider.HeadSounds.SampleData.FileName
                        ), currentHeadSound.HitSounds);
                    }

                    // Update the repeats sounds
                    if (slider.Repeats > 1 && slider.RepeatSounds != null && slider.RepeatSounds.Count == (slider.Repeats - 1))
                    {
                        for (var i = 0; i < slider.Repeats - 1; i++)
                        {
                            var repeatSound = hitSoundTimeline.GetSoundAtTime(TimeSpan.FromMilliseconds(Math.Round(slider.Time.TotalMilliseconds + (slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds) / slider.Repeats * (i + 1))));

                            if (repeatSound != null)
                            {
                                slider.RepeatSounds[i] = (new HitSample(
                                    normalSet: repeatSound.NormalSample,
                                    additionSet: repeatSound.AdditionSample,
                                    0,
                                    0,
                                    slider.RepeatSounds[i].SampleData.FileName
                                ), repeatSound.HitSounds);
                            }
                        }
                    }

                    // Update the tail sound
                    var currentEndSound = hitSoundTimeline.GetSoundAtTime(slider.EndTime);

                    if (currentEndSound != null && (Math.Abs(slider.EndTime.TotalMilliseconds - currentEndSound.Time.TotalMilliseconds) <= leniency))
                    {
                        slider.TailSounds = (new HitSample(
                            normalSet: currentEndSound.NormalSample,
                            additionSet: currentEndSound.AdditionSample,
                            0,
                            0,
                            slider.TailSounds.SampleData.FileName
                        ), currentEndSound.HitSounds);
                    }
                }
                else if (hitObject is Spinner spinner)
                {
                    var currentSound = hitSoundTimeline.GetSoundAtTime(spinner.End);

                    if (currentSound != null && (Math.Abs(spinner.End.TotalMilliseconds - currentSound.Time.TotalMilliseconds) <= leniency))
                    {
                        spinner.HitSounds = (new HitSample(
                            normalSet: currentSound.NormalSample,
                            additionSet: currentSound.AdditionSample,
                            0,
                            0,
                            spinner.HitSounds.SampleData.FileName
                        ), currentSound.HitSounds);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies a hitsound to draggable hitobjects (Sliders) at the specified time.
    /// </summary>
    /// <param name="bodyTimeline"></param>
    /// <param name="leniency"></param>
    public void ApplyDraggableHitsounds(SoundTimeline bodyTimeline, int leniency = 2)
    {
        if (HitObjects == null) return;

        if (HitObjects is Sections.HitObjects section)
        {
            foreach (var hitObject in HitObjects.Objects)
            {
                if (hitObject is Slider slider)
                {
                    var currentBodySound = bodyTimeline.GetSoundAtTime(slider.Time);
                    if (currentBodySound != null && (Math.Abs(slider.Time.TotalMilliseconds - currentBodySound.Time.TotalMilliseconds) <= leniency))
                    {
                        slider.HitSounds = (new HitSample(
                                normalSet: currentBodySound.NormalSample,
                                additionSet: currentBodySound.AdditionSample,
                                0,
                                0,
                                slider.TailSounds.SampleData.FileName
                            ), currentBodySound.HitSounds);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies a SampleSetTimeline to the timing points
    /// </summary>
    /// <param name="timeline"></param>
    /// <param name="leniency"></param>
    public void ApplySampleTimeline(SampleSetTimeline timeline, int leniency = 2)
    {
        if (TimingPoints == null) return;

        if (TimingPoints is TimingPoints section)
        {
            foreach (var timingPoint in section.TimingPointList)
            {
                var sampleSet = timeline.GetSampleAtTime(timingPoint.Time.TotalMilliseconds);

                if (sampleSet == null) continue;

                timingPoint.SampleSet = sampleSet.Sample;
                timingPoint.SampleIndex = (uint)sampleSet.Index;
                timingPoint.Volume = (uint)sampleSet.Volume;
            }

            // Add the missing timing points 
            foreach (var sound in timeline.HitSamples)
            {
                var currentUninherited = section.GetUninheritedTimingPointAt(sound.Time);
                var currentInherited = section.GetInheritedTimingPointAt(sound.Time);

                if (currentUninherited == null) continue;

                if (currentInherited == null)
                {
                    section.TimingPointList.Add(new InheritedTimingPoint(
                        time: TimeSpan.FromMilliseconds(sound.Time),
                        sampleSet: sound.Sample,
                        sampleIndex: (uint)sound.Index,
                        volume: (uint)sound.Volume,
                        effects: currentUninherited.Effects,
                        sliderVelocity: section.GetSliderVelocityAt(sound.Time)
                    ));
                }
            }
        }
    }
}
