namespace BeatmapParser;

/// <summary> Represents a editor section</summary>
public interface IEditor : IEncodable
{
    /// <summary> Time in milliseconds of bookmarks. </summary>
    List<TimeSpan>? Bookmarks { get; set; }

    /// <summary> Distance snap multiplier. </summary>
    double DistanceSpacing { get; set; }

    /// <summary>Beat snap divisor.</summary>
    int BeatDivisor { get; set; }

    /// <summary> Grid size of the editor. </summary>
    int GridSize { get; set; }

    /// <summary> Scale factor for the object timeline.</summary>
    double? TimelineZoom { get; set; }
}