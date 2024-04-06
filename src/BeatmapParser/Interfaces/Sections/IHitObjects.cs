namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the hit objects section of a beatmap.
/// </summary>
public interface IHitObjects : IEncodable
{
    /// <summary>
    /// Represents the list of hit  objects in the beatmap.
    /// </summary>
    public List<IHitObject> Objects { get; set; }
}