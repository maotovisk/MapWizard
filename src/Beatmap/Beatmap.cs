using System.Reflection;
using System.Text;
using BeatmapParser.Sections;

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
                           .Where(line => !string.IsNullOrEmpty(line) || !string.IsNullOrWhiteSpace(line))
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

        return new Beatmap(
            version: formatVersion,
            general: Sections.General.Decode(sections[$"{SectionTypes.General}"]),
            editor: sections.ContainsKey($"{SectionTypes.Editor}") ? Sections.Editor.Decode(sections[$"{SectionTypes.Editor}"]) : null,
            metadata: Sections.Metadata.Decode(sections[$"{SectionTypes.Metadata}"]),
            difficulty: Sections.Difficulty.Decode(sections[$"{SectionTypes.Difficulty}"]),
            colours: sections.ContainsKey($"{SectionTypes.Colours}") ? Sections.Colours.Decode(sections[$"{SectionTypes.Colours}"]) : null,
            events: Sections.Events.Decode(sections[$"{SectionTypes.Events}"]),
            timingPoints: sections.ContainsKey($"{SectionTypes.TimingPoints}") ? Sections.TimingPoints.Decode(sections[$"{SectionTypes.TimingPoints}"]) : null,
            hitObjects: Sections.HitObjects.Decode(sections[$"{SectionTypes.HitObjects}"])
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
}