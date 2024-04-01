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
    public IColours Colours { get; set; }
    /// <summary>
    /// The events section of the beatmap.
    /// </summary>
    public IEvents Events { get; set; }
    /// <summary>
    /// The timing points section of the beatmap.
    /// </summary>
    public ITimingPoints TimingPoints { get; set; }
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
        Colours = new Colours();
        Events = new Events();
        TimingPoints = new TimingPoints();
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
    public Beatmap(int version, IMetadata metadata, IGeneral general, IEditor editor, IDifficulty difficulty, IColours colours, IEvents events, ITimingPoints timingPoints, IHitObjects hitObjects)
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
        try
        {
            if (sections.Count != typeof(IBeatmap).GetProperties().Length) throw new Exception($"Invalid number of sections.");

            var versionSection = sections[$"Begin"];
            var formatVersion = int.Parse(versionSection[0].Replace("osu file format v", string.Empty));
            if (formatVersion != 14)
            {
                throw new Exception($"File format version {formatVersion} is not supported yet.");
            }

            return new Beatmap(
                version: formatVersion,
                general: Sections.General.Decode(sections[$"{SectionTypes.General}"]),
                editor: Sections.Editor.Decode(sections[$"{SectionTypes.Editor}"]),
                metadata: Sections.Metadata.Decode(sections[$"{SectionTypes.Metadata}"]),
                difficulty: Sections.Difficulty.Decode(sections[$"{SectionTypes.Difficulty}"]),
                colours: Sections.Colours.Decode(sections[$"{SectionTypes.Colours}"]),
                events: Sections.Events.Decode(sections[$"{SectionTypes.Events}"]),
                timingPoints: Sections.TimingPoints.Decode(sections[$"{SectionTypes.TimingPoints}"]),
                hitObjects: Sections.HitObjects.Decode(sections[$"{SectionTypes.HitObjects}"])
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse beatmap {ex.Message}\n{ex.StackTrace}");
        }
        return new Beatmap();
    }

    /// <summary>
    /// Converts a .osu file into a <see cref="Beatmap"/> object.
    /// </summary>
    /// <param name="path">Path of tthe beatmap</param>
    /// <returns>Dictionary of sections</returns>
    /// <exception cref="Exception"></exception>
    public static Beatmap Decode(FileInfo path)
    {
        if (!path.Exists) throw new Exception("Beatmap file does not exist.");

        var lines = File.ReadAllLines(path.FullName).ToList();

        if (lines.Count == 0) throw new Exception("Beatmap file is empty.");

        return Decode(SplitSections(lines));
    }

    /// <summary>
    /// Converts a string into a <see cref="Beatmap"/> object.
    /// </summary>
    /// <param name="beatmap"></param>
    /// <returns></returns>
    public static Beatmap Decode(string beatmap)
    {
        var stringLines = beatmap.Split("\r\n").ToList();

        if (stringLines.Count == 0) throw new Exception("Beatmap string is empty.");

        return Decode(SplitSections(stringLines));
    }

    /// <summary>
    /// Splits the sections of a beatmap file into a dictionary containing the section name and the lines of the section.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    private static Dictionary<string, List<string>> SplitSections(List<string> lines)
    {
        Dictionary<string, List<string>> result = [];
        (int index, string name) first = (-1, string.Empty);

        for (var index = 0; index != lines.Count; ++index)
        {
            if (string.IsNullOrEmpty(lines[index])) continue;

            foreach (SectionTypes name in Enum.GetValues(typeof(SectionTypes)))
            {
                if (!lines[index].Contains($"[{name}]")) continue;
                if (first.index == -1)
                {
                    first = (index, name.ToString());
                    if (index != 0) result.Add("Begin", lines[0..index]);
                    continue;
                }
                if (first.index + 1 == index) result.Add(name.ToString(), [lines[index]]);
                else result.Add(name.ToString(), lines[(first.index + 1)..index]);

                first = (index, name.ToString());
            }
        }
        if (lines.Count - first.index != 0)
        {
            result.Add(first.name, lines[first.index..lines.Count]);
        }
        return result;
    }
}