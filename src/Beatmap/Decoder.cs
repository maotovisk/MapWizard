namespace BeatmapParser;

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
}