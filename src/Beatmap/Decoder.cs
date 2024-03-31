using System.Numerics;

namespace Beatmap;

/// <summary>
/// Decodes a osu beatmap file into a <see cref="Beatmap"/>.
/// </summary>
public class BeatmapDecoder
{

    /// <summary>
    /// Splits the sections of a beatmap file into a dictionary.
    /// </summary>
    /// <param name="path">Path of tthe beatmap</param>
    /// <returns>Dictionary of sections</returns>
    /// <exception cref="Exception"></exception>
    public static Dictionary<string, List<string>> SplitSections(FileInfo path)
    {
        if (!path.Exists) throw new Exception("");

        Dictionary<string, List<string>> result = [];
        var lines = File.ReadAllLines(path.FullName).ToList();

        (int index, string name) first = (-1, string.Empty);

        for (var index = 0; index != lines.Count; ++index)
        {
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

    /// <summary>
    /// Decodes a dictionary of sections into a <see cref="Beatmap"/>.
    /// </summary>
    /// <param name="sections"></param>
    /// <returns></returns>
    public static Beatmap Decode(Dictionary<string, List<string>> sections)
    {
        Beatmap beatmap = new();

        try
        {
            if (sections.TryGetValue($"Begin", out List<string>? version)) beatmap.HitObjects = Version.FromData(version);
            if (sections.TryGetValue($"{SectionTypes.General}", out List<string>? general)) beatmap.HitObjects = General.FromData(general);
            if (sections.TryGetValue($"{SectionTypes.Editor}", out List<string>? editor)) beatmap.HitObjects = Editor.FromData(editor);
            if (sections.TryGetValue($"{SectionTypes.Metadata}", out List<string>? metadata)) beatmap.HitObjects = Metadata.FromData(metadata);
            if (sections.TryGetValue($"{SectionTypes.Difficulty}", out List<string>? difficulty)) beatmap.HitObjects = Difficulty.FromData(difficulty);
            if (sections.TryGetValue($"{SectionTypes.Colours}", out List<string>? colours)) beatmap.HitObjects = Colours.FromData(colours);
            if (sections.TryGetValue($"{SectionTypes.Events}", out List<string>? events)) beatmap.HitObjects = Events.FromData(events);
            if (sections.TryGetValue($"{SectionTypes.TimingPoints}", out List<string>? timingPoints)) beatmap.HitObjects = TimingPoints.FromData(timingPoints);
            if (sections.TryGetValue($"{SectionTypes.HitObjects}", out List<string>? hitObjects)) beatmap.HitObjects = HitObjects.FromData(hitObjects);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            beatmap = new();
        }
        return beatmap;
    }

    /// <summary>
    /// Gets the hit object type from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public HitObjectType? GetHitObjectType(int data)
    {
        List<HitObjectType> types = [];
        foreach (HitObjectType name in Enum.GetValues(typeof(HitObjectType)))
        {
            if ((data & (int)name) != 0x000000000) types.Add(name);
        }
        if (types.Count == 1) return types.First();

        return null;
    }
}