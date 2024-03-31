using System.Numerics;

namespace Beatmap;

/// <summary>
/// Represents the colours section of a <see cref="Beatmap"/>.
/// </summary>
public class Colours : IColours
{
    /// <summary>
    /// The slider border colour of the beatmap.
    /// </summary>
    public Vector4 SliderBorder { get; set; }

    /// <summary>
    /// The additive slider track colour of the beatmap.
    /// </summary>
    public Vector4 SliderTrackOverride { get; set; }

    /// <summary>
    /// The list of combo colours of the beatmap.
    /// </summary>
    public List<IComboColour> Combos { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Colours"/> class with the specified parameters.
    /// </summary>
    public Colours(Vector4 sliderBorder, Vector4 sliderTrackOverride, List<IComboColour> combos)
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
}