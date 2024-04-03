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
    public IEditor Editor { get; set; }
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
        Editor = new Editor();
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
    public Beatmap(int version, IMetadata metadata, IGeneral general, IEditor editor, IDifficulty difficulty, IColours? colours, IEvents events, ITimingPoints? timingPoints, IHitObjects hitObjects)
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
    /// <param name="sections"></param>
    /// <returns></returns>
    private static Beatmap Decode(Dictionary<string, List<string>> sections)
    {
        if (Helper.IsWithinProperitesQuantitity<IBeatmap>(sections.Count)) throw new Exception($"Invalid number of sections. Expected {typeof(IBeatmap).GetProperties().Length} but got {sections.Count}.");

        if (!sections.ContainsKey("Begin")) throw new Exception("Invalid beatmap file. Missing version section.");

        var versionSection = sections[$"Begin"];
        var formatVersion = int.Parse(versionSection[0].Replace("osu file format v", string.Empty));

        if (formatVersion != 14) throw new Exception($"File format version {formatVersion} is not supported yet.");

        return new Beatmap(
            version: formatVersion,
            general: Sections.General.Decode(sections[$"{SectionTypes.General}"]),
            editor: Sections.Editor.Decode(sections[$"{SectionTypes.Editor}"]),
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
    public static Beatmap Decode(FileInfo path) => Decode(SplitSections(File.ReadAllText(path.FullName)));

    /// <summary>
    /// Converts a string into a <see cref="Beatmap"/> object.
    /// </summary>
    /// <param name="beatmap"></param>
    /// <returns></returns>
    public static Beatmap Decode(string beatmap) => Decode(SplitSections(beatmap));

    /// <summary>
    /// Splits the sections of a beatmap file into a dictionary containing the section name and the lines of the section.
    /// </summary>
    /// <param name="beatmap"></param>
    /// <returns></returns>
    private static Dictionary<string, List<string>> SplitSections(string beatmap)
    {
        // consider both windows and linux line endings

        var lines = beatmap.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                           .Select(line => line.Trim())
                           .Where(line => !string.IsNullOrEmpty(line) || !string.IsNullOrWhiteSpace(line))
                           .ToList();

        if (lines.Count == 0) throw new Exception("Beatmap is empty.");

        var result = new Dictionary<string, List<string>>();
        var currentSection = string.Empty;

        foreach (var line in lines)
        {
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Trim('[', ']');
                result[currentSection] = new List<string>();
            }
            else if (line.StartsWith("osu file format"))
            {
                result["Begin"] = new List<string> { line };
            }
            else
            {
                result[currentSection].Add(line);
            }
        }

        return result;
    }
}