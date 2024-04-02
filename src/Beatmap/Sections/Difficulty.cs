using System.Globalization;

namespace BeatmapParser.Sections;

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
    /// <summary>
    /// Converts a list of strings to a <see cref="Difficulty"/> object.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static Difficulty Decode(List<string> section)
    {
        Dictionary<string, string> difficulty = [];
        try
        {
            section.ForEach(line =>
            {
                string[] splittedLine = line.Split(':');

                if (splittedLine.Length < 2)
                {
                    throw new Exception("Invalid difficulty section field.");
                }
                if (difficulty.ContainsKey(splittedLine[0].Trim()))
                {
                    throw new Exception("Adding same propriety multiple times.");
                }

                difficulty.Add(splittedLine[0], splittedLine[1]);
            });

            if (Helper.IsWithinProperitesQuantitity<IDifficulty>(difficulty.Count))
            {
                throw new Exception("Invalid Difficulty section lenght.");
            }

            return new Difficulty(
                hpDrainRate: double.Parse(difficulty["HPDrainRate"], CultureInfo.InvariantCulture),
                circleSize: double.Parse(difficulty["CircleSize"], CultureInfo.InvariantCulture),
                overallDifficulty: double.Parse(difficulty["OverallDifficulty"], CultureInfo.InvariantCulture),
                approachRate: double.Parse(difficulty["ApproachRate"], CultureInfo.InvariantCulture),
                sliderMultiplier: double.Parse(difficulty["SliderMultiplier"], CultureInfo.InvariantCulture),
                sliderTickRate: double.Parse(difficulty["SliderTickRate"], CultureInfo.InvariantCulture)
            );
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to parse Difficultty section:\n", ex);
        }
    }

}
