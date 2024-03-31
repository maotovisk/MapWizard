/// <summary>
/// Represents the difficulty section of a beatmap.
/// </summary>
public interface IDifficultySection
{
    /// <summary>
    /// Gets or sets the hp drain rate of the beatmap.
    /// </summary>
    uint HPDrainRate { get; set; }

    /// <summary>
    /// Gets or sets the circle size of the beatmap.
    /// </summary>
    uint CircleSize { get; set; }

    /// <summary>
    /// Gets or sets the overall difficulty of the beatmap.
    /// </summary>
    uint OverallDifficulty { get; set; }

    /// <summary>
    /// Gets or sets the approach rate of the beatmap.
    /// </summary>
    uint ApproachRate { get; set; }

    /// <summary>
    /// Gets or sets the slider multiplier of the beatmap.
    /// </summary>
    uint SliderMultiplier { get; set; }

    /// <summary>
    /// Gets or sets the slider tick rate of the beatmap.
    /// </summary>
    uint SliderTickRate { get; set; }
}