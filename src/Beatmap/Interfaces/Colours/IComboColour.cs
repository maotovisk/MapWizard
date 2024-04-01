using System.Numerics;

namespace Beatmap;

/// <summary>
/// Represents the difficulty section of a beatmap.
/// </summary>
public interface IComboColour
{
    /// <summary>
    /// Gets or sets the number of the combo colour of the beatmap.
    /// </summary>
    uint Number { get; set; }

    /// <summary>
    /// Gets or sets the colour of the combo colour of the beatmap.
    /// </summary>
    Vector4 Colour { get; set; }
}