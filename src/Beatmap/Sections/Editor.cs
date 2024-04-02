using System.Globalization;

namespace BeatmapParser.Sections;

/// <summary> Represents a editor section</summary>
public class Editor : IEditor
{
    /// <summary> Time in milliseconds of bookmarks. </summary>
    public List<TimeSpan>? Bookmarks { get; set; }

    /// <summary> Distance snap multiplier. </summary>
    public double DistanceSpacing { get; set; }

    /// <summary>Beat snap divisor.</summary>
    public int BeatDivisor { get; set; }

    /// <summary> Grid size of the editor. </summary>
    public int GridSize { get; set; }

    /// <summary> Scale factor for the object timeline.</summary>
    public double TimelineZoom { get; set; }

    /// <summary>
    /// Contructs a new instance of the <see cref="Editor"/> class.
    /// </summary>
    /// <param name="bookmarks"></param>
    /// <param name="distanceSpacing"></param>
    /// <param name="beatDivisor"></param>
    /// <param name="gridSize"></param>
    /// <param name="timelineZoom"></param>
    public Editor(List<TimeSpan>? bookmarks, double distanceSpacing, int beatDivisor, int gridSize, double timelineZoom)
    {
        Bookmarks = bookmarks;
        DistanceSpacing = distanceSpacing;
        BeatDivisor = beatDivisor;
        GridSize = gridSize;
        TimelineZoom = timelineZoom;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Editor"/> class.
    /// </summary>
    public Editor() { }

    /// <summary>
    /// Converts a list of strings to a <see cref="Editor"/> object.
    /// </summary>
    /// <param name="sections"></param>
    /// <returns></returns>
    public static Editor Decode(List<string> sections)
    {
        Dictionary<string, string> editor = [];
        try
        {
            sections.ForEach(line =>
            {
                string[] splittedLine = line.Split(':');

                if (splittedLine.Length < 2)
                {
                    throw new Exception("Invalid editor section field.");
                }

                if (editor.ContainsKey(splittedLine[0].Trim()))
                {
                    throw new Exception("Adding same propriety multiple times.");
                }

                // Account for mutiple ':' in the value
                editor.Add(splittedLine[0].Trim(), string.Join(":", splittedLine.Skip(1)).Trim());
            });

            if (Helper.IsWithinProperitesQuantitity<IEditor>(editor.Count))
            {
                throw new Exception("Invalid Editor section length. Missing properties: " + string.Join(", ", Helper.GetMissingPropertiesNames<IEditor>(editor.Keys)) + ".");
            }

            editor.TryGetValue("Bookmarks", out string? bookmarks);

            return new Editor(
                bookmarks: bookmarks?.Split(',').Select(x => TimeSpan.FromMilliseconds(double.Parse(x, CultureInfo.InvariantCulture))).ToList(),
                distanceSpacing: double.Parse(editor["DistanceSpacing"], CultureInfo.InvariantCulture),
                beatDivisor: int.Parse(editor["BeatDivisor"]),
                gridSize: int.Parse(editor["GridSize"]),
                timelineZoom: double.Parse(editor["TimelineZoom"], CultureInfo.InvariantCulture)
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing Editor section:\n{ex}.");
        }
    }

}