using System.Numerics;

namespace MapWizard.BeatmapParser;

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
    public Vector3 Colour { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="number"></param>
    /// <param name="colour"></param>
    public ComboColour(uint number, Vector3 colour)
    {
        Number = number;
        Colour = colour;
    }

    /// <summary>
    /// Converts a string to a <see cref="ComboColour"/>.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static ComboColour Decode(string line)
    {
        try
        {
            string[] split = line.Split(':');
            return new ComboColour(uint.Parse(split[0].Replace("Combo", string.Empty)), Helper.ParseVector3(split[1]));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse ComboColour {ex.Message}\n{ex.StackTrace}");
        }
    }
}