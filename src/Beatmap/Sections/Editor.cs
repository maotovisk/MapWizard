using System.Globalization;
using System.Text;

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
    public double? TimelineZoom { get; set; }

    /// <summary>
    /// Contructs a new instance of the <see cref="Editor"/> class.
    /// </summary>
    /// <param name="bookmarks"></param>
    /// <param name="distanceSpacing"></param>
    /// <param name="beatDivisor"></param>
    /// <param name="gridSize"></param>
    /// <param name="timelineZoom"></param>
    public Editor(List<TimeSpan>? bookmarks, double distanceSpacing, int beatDivisor, int gridSize, double? timelineZoom)
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
                string[] splittedLine = line.Split(':', 2);

                if (splittedLine.Length < 1)
                {
                    throw new Exception("Invalid editor section field.");
                }

                // Account for mutiple ':' in the value
                editor.Add(splittedLine[0].Trim(), splittedLine.Length != 1 ? splittedLine[1].Trim() : string.Empty);
            });

            if (Helper.IsWithinProperitesQuantitity<IEditor>(editor.Count))
            {
                throw new Exception("Invalid Editor section length. Missing properties: " + string.Join(", ", Helper.GetMissingPropertiesNames<IEditor>(editor.Keys)) + ".");
            }

            editor.TryGetValue("Bookmarks", out string? bookmarks);

            return new Editor(
                bookmarks: bookmarks?.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(
                    x => TimeSpan.FromMilliseconds(double.Parse(x, CultureInfo.InvariantCulture))).ToList(),
                distanceSpacing: editor.TryGetValue("DistanceSpacing", out string? value) ? double.Parse(value, CultureInfo.InvariantCulture) : 1.0,
                beatDivisor: int.Parse(editor["BeatDivisor"]),
                gridSize: int.Parse(editor["GridSize"]),
                timelineZoom: editor.TryGetValue("TimelineZoom", out var timelineZoom) ? double.Parse(timelineZoom, CultureInfo.InvariantCulture) : null
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing Editor section:\n{ex}.");
        }
    }

    /// <summary>
    /// Encodes the <see cref="Editor"/> section into a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        foreach (var prop in typeof(IEditor).GetProperties())
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

            if (prop.GetValue(this) is List<TimeSpan> bookmarks)
            {
                builder.AppendLine($"{prop.Name}: {string.Join(',', bookmarks.Select(x => x.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)))}");
                continue;
            }

            builder.AppendLine($"{prop.Name}: {prop.GetValue(this)}");
        }

        return builder.ToString();
    }

}