namespace Beatmap;

/// <summary>
/// Represents an inherited timing point of the beatmap.
/// </summary>
public interface IInheritedTimingPoint : ITimingPoint
{
    /// <summary>
    /// Gets or sets the slider velocity of the beatmap.
    /// </summary>
    double SliderVelocity { get; set; }
}