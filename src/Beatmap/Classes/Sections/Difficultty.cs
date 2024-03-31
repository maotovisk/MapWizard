namespace Beatmap;

/// <summary>
/// Represents the difficulty section of a <see cref="Beatmap"/>.
/// </summary>
public class Difficulty : IDifficulty
{
    /// <summary>
    /// The HP drain rate of the beatmap.
    /// </summary>
    public double HPDrainRate { get; set; }

    /// <summary>
    /// The circle size of the beatmap.
    /// </summary>
    public double CircleSize { get; set; }

    /// <summary>
    /// The overall difficulty of the beatmap.
    /// </summary>
    public double OverallDifficulty { get; set; }

    /// <summary>
    /// The approach rate of the beatmap.
    /// </summary>
    public double ApproachRate { get; set; }

    /// <summary>
    /// The slider velocity multiplier of the beatmap.
    /// </summary>
    public double SliderMultiplier { get; set; }

    /// <summary>
    /// The slider tick rate of the beatmap.
    /// </summary>
    public double SliderTickRate { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Difficulty"/> class.
    /// </summary>
    /// <param name="hpDrainRate"></param>
    /// <param name="circleSize"></param>
    /// <param name="overallDifficulty"></param>
    /// <param name="approachRate"></param>
    /// <param name="sliderMultiplier"></param>
    /// <param name="sliderTickRate"></param>
    public Difficulty(
        double hpDrainRate,
        double circleSize,
        double overallDifficulty,
        double approachRate,
        double sliderMultiplier,
        double sliderTickRate
    )
    {
        HPDrainRate = hpDrainRate;
        CircleSize = circleSize;
        OverallDifficulty = overallDifficulty;
        ApproachRate = approachRate;
        SliderMultiplier = sliderMultiplier;
        SliderTickRate = sliderTickRate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Difficulty"/> class.
    /// </summary>
    public Difficulty()
    {
        HPDrainRate = 5;
        CircleSize = 5;
        OverallDifficulty = 5;
        ApproachRate = 5;
        SliderMultiplier = 1;
        SliderTickRate = 1;
    }
    public Difficulty FromData(ref Beatmap beatmap, List<string> section)
    {
        return new Difficulty()
        {
            HPDrainRate = double.Parse(section[0]),
            CircleSize = double.Parse(section[1]),
            OverallDifficulty = double.Parse(section[2]),
            ApproachRate = double.Parse(section[3]),
            SliderMultiplier = double.Parse(section[4]),
            SliderTickRate = double.Parse(section[5])
        };
    }

}
