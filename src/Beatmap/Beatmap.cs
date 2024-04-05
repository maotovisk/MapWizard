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
    /// <param name="time"></param>
    /// <param name="hitSound"></param>
    /// <param name="leniency"></param>
    public void ApplyNonDraggableHitsoundAt(double time, SoundEvent hitSound, int leniency = 2)
    {
        if (HitObjects == null) return;
        if (TimingPoints == null) return;

        if (HitObjects is Sections.HitObjects section)
        {
            var hitObject = section.GetHitObjectAt(time);
            if (hitObject == null) return;

            if (hitObject is Circle circle)
            {
                // We want to change the hitsound of the circle
                circle.HitSounds = (new HitSample(
                    normalSet: hitSound.NormalSample,
                    additionSet: hitSound.AdditionSample,
                    circle.HitSounds.SampleData.Index,
                    circle.HitSounds.SampleData.Volume,
                    circle.HitSounds.SampleData.FileName
                ), hitSound.HitSounds);

                HitObjects.Objects[HitObjects.Objects.IndexOf(hitObject)] = circle;
            }
            else if (hitObject is Slider slider)
            {
                // Since we are not changing the hitsound of the slider, we will change the hitsound of the head
                if (Math.Abs(time - slider.Time.TotalMilliseconds) <= leniency)
                {
                    slider.HitSounds = (new HitSample(
                        normalSet: hitSound.NormalSample,
                        additionSet: hitSound.AdditionSample,
                        slider.HeadSounds.SampleData.Index,
                        slider.HeadSounds.SampleData.Volume,
                        slider.HeadSounds.SampleData.FileName
                    ), hitSound.HitSounds);
                }
                else
                {
                    // We want to change the hitsound of the repeats, so we need to calculate the time of the repeats based on the number of repeats and the difference between the end time and the start time of the slider
                    for (int i = 1; i < slider.Repeats; i++)
                    {
                        var repeatTime = (slider.EndTime - slider.Time) / slider.Repeats * i + slider.Time;
                        if (Math.Abs(repeatTime.TotalMilliseconds - time) <= leniency && slider.RepeatSounds != null && slider.RepeatSounds.Count == slider.Repeats)
                        {
                            var oldSounds = slider.RepeatSounds;
                            oldSounds[i] = (new HitSample(
                                normalSet: hitSound.NormalSample,
                                additionSet: hitSound.AdditionSample,
                                index: oldSounds[i].SampleData.Index,
                                volume: oldSounds[i].SampleData.Volume,
                                fileName: oldSounds[i].SampleData.FileName
                            ), hitSound.HitSounds);

                            slider.RepeatSounds = oldSounds;
                        }
                    }

                    if (Math.Abs(time - slider.EndTime.TotalMilliseconds) <= leniency)
                    {
                        slider.TailSounds = (new HitSample(
                            normalSet: hitSound.NormalSample,
                            additionSet: hitSound.AdditionSample,
                            slider.TailSounds.SampleData.Index,
                            slider.TailSounds.SampleData.Volume,
                            slider.TailSounds.SampleData.FileName
                        ), hitSound.HitSounds);
                    }
                }

                HitObjects.Objects[HitObjects.Objects.IndexOf(hitObject)] = slider;
            }
            else if (hitObject is Spinner spinner)
            {
                spinner.HitSounds = (new HitSample(
                            normalSet: hitSound.NormalSample,
                            additionSet: hitSound.AdditionSample,
                            spinner.HitSounds.SampleData.Index,
                            spinner.HitSounds.SampleData.Volume,
                            spinner.HitSounds.SampleData.FileName
                        ), hitSound.HitSounds);
                HitObjects.Objects[HitObjects.Objects.IndexOf(hitObject)] = spinner;
            }

        }
    }

    /// <summary>
    /// Applies a hitsound to draggable hitobjects (Sliders) at the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="hitSound"></param>
    /// <param name="leniency"></param>
    public void ApplyDraggableHitsoundAt(double time, SoundEvent hitSound, int leniency = 2)
    {
        if (HitObjects == null) return;

        if (HitObjects is Sections.HitObjects section)
        {
            var hitObject = section.GetHitObjectAt(time);

            if (hitObject is Slider slider && (Math.Abs(slider.Time.TotalMilliseconds - time) <= leniency))
            {
                slider.HitSounds = (new HitSample(
                    normalSet: hitSound.NormalSample,
                    additionSet: hitSound.AdditionSample,
                    slider.HitSounds.SampleData.Index,
                    slider.HitSounds.SampleData.Volume,
                    slider.HitSounds.SampleData.FileName
                ), hitSound.HitSounds);

                HitObjects.Objects[HitObjects.Objects.IndexOf(hitObject)] = slider;
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

            var newTimingPoints = new List<ITimingPoint>();
            foreach (var sampleSetEvent in timeline.HitSamples)
            {
                var timingPoint = section.GetTimingPointAt(sampleSetEvent.Time);

                if (timingPoint != null)
                {
                    var currentSliderVelocity = section.GetSliderVelocityAt(sampleSetEvent.Time);
                    if (timingPoint.Time.TotalMilliseconds < sampleSetEvent.Time)
                    {
                        var inheritedTimingPoint = new InheritedTimingPoint(TimeSpan.FromMilliseconds(sampleSetEvent.Time), sampleSetEvent.Sample, (uint)sampleSetEvent.Index, (uint)sampleSetEvent.Volume, timingPoint.Effects, currentSliderVelocity);
                        newTimingPoints.Add(inheritedTimingPoint);
                    }
                    else
                    {
                        timingPoint.SampleSet = sampleSetEvent.Sample;
                        timingPoint.SampleIndex = (uint)sampleSetEvent.Index;
                        timingPoint.Volume = (uint)sampleSetEvent.Volume;

                        newTimingPoints.Add(timingPoint);
                    }
                }
            }

            foreach (var timingPoint in section.TimingPointList)
            {

                var sampleSet = timeline.GetSampleAtTime(timingPoint.Time.TotalMilliseconds, leniency);

                var timingPointCopy = newTimingPoints.FirstOrDefault(x => x.Time.TotalMilliseconds == timingPoint.Time.TotalMilliseconds);

                if (sampleSet != null && timingPointCopy != null)
                {
                    timingPointCopy.SampleSet = sampleSet.Sample;
                    timingPointCopy.SampleIndex = (uint)sampleSet.Index;
                    timingPointCopy.Volume = (uint)sampleSet.Volume;
                }
            }

            TimingPoints.TimingPointList = [.. newTimingPoints.OrderBy(x => x.Time.TotalMilliseconds)];
        }
    }
}
