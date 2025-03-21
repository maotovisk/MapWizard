using System.Drawing;
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
    public Color? SliderBorder { get; set; }

    /// <summary>
    /// The additive slider track colour of the beatmap.
    /// </summary>
    public Color? SliderTrackOverride { get; set; }

    /// <summary>
    /// The list of combo colours of the beatmap.
    /// </summary>
    public List<ComboColour> Combos { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Colours"/> class with the specified parameters.
    /// </summary>
    public Colours(Color sliderBorder, Color sliderTrackOverride, List<ComboColour> combos)
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
                    result.SliderBorder = Helper.ParseColor(split[1].Trim());
                }
                else if (sectionLine.StartsWith("SliderTrackOverride"))
                {
                    var split = sectionLine.Split(':', 2);
                    result.SliderTrackOverride = Helper.ParseColor(split[1].Trim());
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
            builder.AppendLine($"Combo{combo.Number} : {combo.Colour.R},{combo.Colour.G},{combo.Colour.B}");
        });
        
        if (SliderBorder.HasValue)
        {
            builder.AppendLine($"SliderBorder : {SliderBorder.Value.R},{SliderBorder.Value.G},{SliderBorder.Value.B}");
        }
        
        if (SliderTrackOverride.HasValue)
        {
            builder.AppendLine($"SliderTrackOverride : {SliderTrackOverride.Value.R},{SliderTrackOverride.Value.G},{SliderTrackOverride.Value.B}");
        }
        
        return builder.ToString();
    }

}