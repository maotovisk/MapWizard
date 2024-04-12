namespace MapWizard.BeatmapParser;

/// <summary>
/// Specifies that the object can be encoded into a string that complies with the osu! file format.
/// </summary>
public interface IEncodable
{
    /// <summary>
    /// Encodes the object into a string that complies with the osu! file format.
    /// </summary>
    /// <returns></returns>
    public string Encode();
}