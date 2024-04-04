namespace BeatmapParser;
/// <summary>
/// Represents a beatmap.
/// </summary>
public interface IBeatmap : IEncodable
{
    /// <summary>
    /// Gets or sets the version of the beatmap.
    /// </summary>
    int Version { get; set; }

    /// <summary>
    /// Gets or sets the metadata of the beatmap.
    /// </summary>
    IMetadata Metadata { get; set; }

    /// <summary>
    /// Gets or sets the general settings of the beatmap.
    /// </summary>
    IGeneral General { get; set; }

    /// <summary>
    /// Gets or sets the editor settings of the beatmap.
    /// </summary>
    IEditor? Editor { get; set; }

    /// <summary>
    /// Gets or sets the difficulty settings of the beatmap.
    /// </summary>
    IDifficulty Difficulty { get; set; }

    /// <summary>
    /// Gets or sets the colours used in the beatmap.
    /// </summary>
    IColours? Colours { get; set; }

    /// <summary>
    /// Gets or sets the events in the beatmap.
    /// </summary>
    IEvents Events { get; set; }

    /// <summary>
    /// Gets or sets the timing points in the beatmap.
    /// </summary>
    ITimingPoints? TimingPoints { get; set; }

    /// <summary>
    /// Gets or sets the hit objects in the beatmap.
    /// </summary>
    IHitObjects HitObjects { get; set; }
}