using System.Numerics;

namespace MapWizard.BeatmapParser;

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
    Vector3 Colour { get; set; }
}