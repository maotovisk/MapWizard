using System.Numerics;

namespace BeatmapParser;

/// <summary>
/// Converter for Vectors.
/// </summary>
public static class VectorConverter
{

    /// <summary>
    /// Returns a Vector3 from a string.
    /// </summary>
    /// <param name="vectorString"></param>
    /// <returns></returns>
    public static Vector3 ToVector3(string vectorString)
    {
        string[] split = vectorString.Split(',');
        return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
    }
}