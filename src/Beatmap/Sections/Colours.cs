using System.Numerics;

namespace BeatmapParser.Sections;

/// <summary>
/// Represents the colours section of a <see cref="Beatmap"/>.
/// </summary>
public class Colours : IColours
{
    /// <summary>
    /// The slider border colour of the beatmap.
    /// </summary>
    public Vector3? SliderBorder { get; set; }

    /// <summary>
    /// The additive slider track colour of the beatmap.
    /// </summary>
    public Vector3? SliderTrackOverride { get; set; }

    /// <summary>
    /// The list of combo colours of the beatmap.
    /// </summary>
    public List<IComboColour> Combos { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Colours"/> class with the specified parameters.
    /// </summary>
    public Colours(Vector3 sliderBorder, Vector3 sliderTrackOverride, List<IComboColour> combos)
    {
        SliderBorder = sliderBorder;
        SliderTrackOverride = sliderTrackOverride;
        Combos = combos;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Colours"/> class.
    /// </summary>
    public Colours()
    {
        Combos = new List<IComboColour>();
    }

    /// <summary>
    /// Parses a list of Colours lines into a new <see cref="Colours"/> class.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static Colours Decode(List<string> section)
    {
        Colours result = new Colours();
        try
        {
            section.ForEach(sectionLine =>
            {
                if (sectionLine.StartsWith("SliderBorder:"))
                {
                    string[] split = sectionLine.Split(':');
                    result.SliderBorder = Helper.ParseVector3(split[1]);
                }
                else if (sectionLine.StartsWith("SliderTrackOverride:"))
                {
                    string[] split = sectionLine.Split(':');
                    result.SliderTrackOverride = Helper.ParseVector3(split[1]);
                }
                else if (sectionLine.StartsWith("Combo"))
                {
                    result.Combos.Add(ComboColour.Decode(sectionLine));
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to parse Colours section.", ex);
        }
    }
}