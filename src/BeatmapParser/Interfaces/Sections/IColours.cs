namespace MapWizard.BeatmapParser;

using System.Numerics;

/// <summary>
/// Represents the colours section of a beatmap.
/// </summary>
public interface IColours : IEncodable
{
    /// <summary>
    /// Gets or sets the slider border colour of the beatmap.
    /// </summary>
    Vector3? SliderBorder { get; set; }

    /// <summary>
    /// Gets or sets the additive slider track colour of the beatmap.
    /// </summary>
    Vector3? SliderTrackOverride { get; set; }

    /// <summary>
    /// Represents the difficulty section of a beatmap.
    /// </summary>
    List<IComboColour> Combos { get; set; }
}