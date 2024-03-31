namespace Beatmap;

/// <summary>
/// Represents the hit objects section of a beatmap.
/// </summary>
public interface IHitObjects
{
    /// <summary>
    /// Represents the list of hit  objects in the beatmap.
    /// </summary>
    public List<IHitObject> Objects { get; set; }
}