using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the difficulty section of a <see cref="Beatmap"/>.
/// </summary>
public class Difficulty
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
    public double? ApproachRate { get; set; }

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
    private Difficulty(
        double hpDrainRate,
        double circleSize,
        double overallDifficulty,
        double? approachRate,
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
                var splitLine = line.Split(':', 2);

                if (splitLine.Length < 1)
                {
                    throw new Exception("Invalid difficulty section field.");
                }

                difficulty[splitLine[0]] = splitLine.Length != 1 ? splitLine[1].Trim() : string.Empty;
            });

            foreach (var key in new[] { "HPDrainRate", "CircleSize", "OverallDifficulty", "SliderMultiplier", "SliderTickRate" })
            {
                if (!difficulty.ContainsKey(key))
                    throw new Exception($"Difficulty section missing required field: {key}");
            }

            return new Difficulty(
                hpDrainRate: double.Parse(difficulty["HPDrainRate"], CultureInfo.InvariantCulture),
                circleSize: double.Parse(difficulty["CircleSize"], CultureInfo.InvariantCulture),
                overallDifficulty: double.Parse(difficulty["OverallDifficulty"], CultureInfo.InvariantCulture),
                approachRate: difficulty.TryGetValue("ApproachRate", out var approachRate) ? double.Parse(approachRate, CultureInfo.InvariantCulture) : double.Parse(difficulty["OverallDifficulty"], CultureInfo.InvariantCulture),
                sliderMultiplier: double.Parse(difficulty["SliderMultiplier"], CultureInfo.InvariantCulture),
                sliderTickRate: double.Parse(difficulty["SliderTickRate"], CultureInfo.InvariantCulture)
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Difficulty section:\n {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Encodes the <see cref="Difficulty"/> section into a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        foreach (var property in typeof(Difficulty).GetProperties())
        {
            if (property.GetValue(this) is null) continue;

            if (property.GetValue(this) is bool boolValue)
            {
                builder.AppendLine($"{property.Name}:{(boolValue ? 1 : 0)}");
                continue;
            }

            if (property.GetValue(this) is double doubleValue)
            {
                builder.AppendLine($"{property.Name}:{doubleValue.ToString(CultureInfo.InvariantCulture)}");
                continue;
            }

            builder.AppendLine($"{property.Name}:{property.GetValue(this)}");
        }

        return builder.ToString();
    }

}
