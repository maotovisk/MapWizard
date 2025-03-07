using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the colours section of a <see cref="Beatmap"/>.
/// </summary>
public class Colours
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
    public List<ComboColour> Combos { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Colours"/> class with the specified parameters.
    /// </summary>
    public Colours(Vector3 sliderBorder, Vector3 sliderTrackOverride, List<ComboColour> combos)
    {
        SliderBorder = sliderBorder;
        SliderTrackOverride = sliderTrackOverride;
        Combos = combos;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Colours"/> class.
    /// </summary>
    private Colours()
    {
        Combos = [];
    }

    /// <summary>
    /// Parses a list of Colours lines into a new <see cref="Colours"/> class.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static Colours Decode(List<string> section)
    {
        Colours result = new();
        try
        {
            section.ForEach(sectionLine =>
            {
                if (sectionLine.StartsWith("SliderBorder"))
                {
                    var split = sectionLine.Split(':', 2);
                    result.SliderBorder = Helper.ParseVector3(split[1].Trim());
                }
                else if (sectionLine.StartsWith("SliderTrackOverride"))
                {
                    var split = sectionLine.Split(':', 2);
                    result.SliderTrackOverride = Helper.ParseVector3(split[1].Trim());
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

    /// <summary>
    /// Encodes the <see cref="Colours"/> class into a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        Combos.ForEach(combo =>
        {
            builder.AppendLine($"Combo{combo.Number} : {combo.Colour.X},{combo.Colour.Y},{combo.Colour.Z}");
        });
        
        if (SliderBorder.HasValue)
        {
            builder.AppendLine($"SliderBorder : {SliderBorder.Value.X},{SliderBorder.Value.Y},{SliderBorder.Value.Z}");
        }
        
        if (SliderTrackOverride.HasValue)
        {
            builder.AppendLine($"SliderTrackOverride : {SliderTrackOverride.Value.X},{SliderTrackOverride.Value.Y},{SliderTrackOverride.Value.Z}");
        }
        
        return builder.ToString();
    }

}