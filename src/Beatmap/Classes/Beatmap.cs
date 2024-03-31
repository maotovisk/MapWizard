
namespace Beatmap;

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
    private static void Version(List<string> section)
    {
        int.Parse(section[0]);
    }

}

