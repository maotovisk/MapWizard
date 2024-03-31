using System.Numerics;

namespace Beatmap;

/// <summary>
/// Represents the difficulty section of a beatmap.
/// </summary>
public class ComboColour : IComboColour
{
    /// <summary>
    /// Gets or sets the number of the combo colour of the beatmap.
    /// </summary>
    public uint Number { get; set; }

    /// <summary>
    /// Gets or sets the colour of the combo colour of the beatmap.
    /// </summary>
    public Vector4 Colour { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="number"></param>
    /// <param name="colour"></param>
    public ComboColour(uint number, Vector4 colour)
    {
        Number = number;
        Colour = colour;
    }
}