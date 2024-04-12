using System.Text;
using MapWizard.BeatmapParser.Sections;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents an osu! beatmap.
/// </summary>
public class Beatmap : IEncodable
{
    /// <summary>
    /// The format version of the beatmap.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The metadata section of the beatmap.
    /// </summary>
    public Metadata Metadata { get; set; }
    /// <summary>
    /// The general section of the beatmap.
    /// </summary>
    public General General { get; set; }
    /// <summary>
    /// The editor section of the beatmap.
    /// </summary>
    public Editor? Editor { get; set; }
    /// <summary>
    /// The difficulty section of the beatmap.
    /// </summary>
    public Difficulty Difficulty { get; set; }
    /// <summary>
    /// The colours section of the beatmap.
    /// </summary>
    public Colours? Colours { get; set; }
    /// <summary>
    /// The events section of the beatmap.
    /// </summary>
    public Events Events { get; set; }
    /// <summary>
    /// The timing points section of the beatmap.
    /// </summary>
    public TimingPoints? TimingPoints { get; set; }
    /// <summary>
    /// The hit objects section of the beatmap.
    /// </summary>
    public HitObjects HitObjects { get; set; }

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
    private Beatmap(int version, Metadata metadata, General general, Editor? editor, Difficulty difficulty, Colours? colours, Events events, TimingPoints? timingPoints, HitObjects hitObjects)
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

        if (!lines[0].Contains("file format")) throw new Exception("Invalid file format.");

        var formatVersion = int.Parse(lines[0].Split("v")[1].Trim());

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

        if (Helper.IsWithinPropertyQuantity<Beatmap>(sections.Count)) throw new Exception($"Invalid number of sections. Expected {typeof(Beatmap).GetProperties().Length} but got {sections.Count}.");

        var general = Sections.General.Decode(sections[$"{SectionTypes.General}"]);
        var editor = sections.ContainsKey($"{SectionTypes.Editor}") ? Sections.Editor.Decode(sections[$"{SectionTypes.Editor}"]) : null;
        var metadata = Sections.Metadata.Decode(sections[$"{SectionTypes.Metadata}"]);
        var difficulty = Sections.Difficulty.Decode(sections[$"{SectionTypes.Difficulty}"]);
        var colours = sections.ContainsKey($"{SectionTypes.Colours}") ? Sections.Colours.Decode(sections[$"{SectionTypes.Colours}"]) : null;
        var events = Sections.Events.Decode(sections[$"{SectionTypes.Events}"]);
        var timingPoints = sections.ContainsKey($"{SectionTypes.TimingPoints}") ? Sections.TimingPoints.Decode(sections[$"{SectionTypes.TimingPoints}"]) : null;
        var hitObjects = Sections.HitObjects.Decode(sections[$"{SectionTypes.HitObjects}"], timingPoints ?? new TimingPoints(), difficulty);

        return new Beatmap(
            formatVersion, metadata, general, editor, difficulty, colours, events, timingPoints, hitObjects
        );
    }

    /// <summary>
    /// Converts a .osu file into a <see cref="Beatmap"/> object.
    /// </summary>
    /// <param name="path">Path of the beatmap</param>
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
            builder.AppendLine(Events.Encode());

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
        builder.Append(HitObjects.Encode());

        return builder.ToString();
    }

    /// <summary>
    /// Gets the uninherited timing point at the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public UninheritedTimingPoint? GetUninheritedTimingPointAt(double time)
    {
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
        return ((HitObjects)HitObjects).GetHitObjectAt(time, leniency);
    }
}
