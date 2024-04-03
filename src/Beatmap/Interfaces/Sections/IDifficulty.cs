namespace BeatmapParser;

/// <summary>
/// Represents the difficulty section of a beatmap.
/// </summary>
public interface IDifficulty
{
    /// <summary>
    /// Gets or sets the hp drain rate of the beatmap.
    /// </summary>
    double HPDrainRate { get; set; }

    /// <summary>
    /// Gets or sets the circle size of the beatmap.
    /// </summary>
    double CircleSize { get; set; }

    /// <summary>
    /// Gets or sets the overall difficulty of the beatmap.
    /// </summary>
    double OverallDifficulty { get; set; }

    /// <summary>
    /// Gets or sets the approach rate of the beatmap.
    /// </summary>
    double? ApproachRate { get; set; }

    /// <summary>
    /// Gets or sets the slider multiplier of the beatmap.
    /// </summary>
    double SliderMultiplier { get; set; }

    /// <summary>
    /// Gets or sets the slider tick rate of the beatmap.
    /// </summary>
    double SliderTickRate { get; set; }
}