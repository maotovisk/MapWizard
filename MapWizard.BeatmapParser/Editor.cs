using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary> Represents a editor section</summary>
public class Editor
{
    private List<double>? _bookmarkMilliseconds;

    /// <summary> Time in milliseconds of bookmarks. </summary>
    public List<TimeSpan>? Bookmarks
    {
        get => _bookmarkMilliseconds?.Select(TimeSpan.FromMilliseconds).ToList();
        set => _bookmarkMilliseconds = value?.Select(x => x.TotalMilliseconds).ToList();
    }

    /// <summary> Distance snap multiplier. </summary>
    public double DistanceSpacing { get; set; }

    /// <summary>Beat snap divisor.</summary>
    public int BeatDivisor { get; set; }

    /// <summary> Grid size of the editor. </summary>
    public int GridSize { get; set; }

    /// <summary> Scale factor for the object timeline.</summary>
    public double? TimelineZoom { get; set; }

    /// <summary>
    /// Constructs a new instance of the <see cref="Editor"/> class.
    /// </summary>
    /// <param name="bookmarks"></param>
    /// <param name="distanceSpacing"></param>
    /// <param name="beatDivisor"></param>
    /// <param name="gridSize"></param>
    /// <param name="timelineZoom"></param>
    private Editor(List<double>? bookmarks, double distanceSpacing, int beatDivisor, int gridSize, double? timelineZoom)
    {
        _bookmarkMilliseconds = bookmarks;
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
                var splitLine = line.Split(':', 2);

                if (splitLine.Length < 1)
                {
                    throw new Exception("Invalid editor section field.");
                }

                editor[splitLine[0].Trim()] = splitLine.Length != 1 ? splitLine[1].Trim() : string.Empty;
            });

            editor.TryGetValue("Bookmarks", out var bookmarks);

            return new Editor(
                bookmarks: bookmarks?.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(
                    x => double.Parse(x, CultureInfo.InvariantCulture)).ToList(),
                distanceSpacing: editor.TryGetValue("DistanceSpacing", out string? value) ? double.Parse(value, CultureInfo.InvariantCulture) : 1.0,
                beatDivisor: editor.TryGetValue("BeatDivisor", out var beatDivisorStr) ? int.Parse(beatDivisorStr) : 4,
                gridSize: editor.TryGetValue("GridSize", out var gridSizeStr) ? int.Parse(gridSizeStr) : 4,
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

        foreach (var prop in typeof(Editor).GetProperties())
        {
            if (prop.GetValue(this) is null) continue;
            if (prop.GetValue(this) is string str && string.IsNullOrEmpty(str) && Helper.FormatVersion == 128) continue;

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

            if (prop.Name == nameof(Bookmarks) && _bookmarkMilliseconds is not null)
            {
                builder.AppendLine($"{prop.Name}: {string.Join(',', _bookmarkMilliseconds.Select(Helper.FormatTime))}");
                continue;
            }

            builder.AppendLine($"{prop.Name}: {prop.GetValue(this)}");
        }

        return builder.ToString();
    }

}
