namespace Beatmap;

/// <summary> Represents a editor section</summary>
public class Editor : IEditor
{
    /// <summary> Time in milliseconds of bookmarks. </summary>
    public List<TimeSpan> Bookmarks { get; set; }

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
    public Editor(List<TimeSpan> bookmarks, double distanceSpacing, int beatDivisor, int gridSize, double timelineZoom)
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
    public Editor()
    {
        Bookmarks = new List<TimeSpan>();
    }
    /// <summary>
    /// Converts a list of strings to a <see cref="Editor"/> object.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static Editor FromData(List<string> section)
    {
        return new Editor();
    }

}