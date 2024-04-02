using System.Globalization;
using System.Numerics;

namespace BeatmapParser;

/// <summary>
/// Generic helper methods.
/// </summary>
public partial class Helper
{
    /// <summary>
    /// Converts integer bitwise into a <see cref="HitSound"/> list.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<HitSound> ParseHitSounds(int data)
    {
        List<HitSound> hitSounds = [];
        foreach (HitSound name in Enum.GetValues(typeof(HitSound)))
        {
            if ((data & (int)name) != 00000000) hitSounds.Add(name);
        }
        return hitSounds;
    }

    /// <summary>
    /// Get the a list of effects from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<Effect> ParseEffects(int data)
    {
        List<Effect> types = [];
        foreach (Effect name in Enum.GetValues(typeof(Effect)))
        {
            if ((data & (int)name) != 00000000) types.Add(name);
        }
        return types;
    }

    /// <summary>
    /// Gets the hit object type from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static HitObjectType ParseHitObjectType(int data)
    {
        List<HitObjectType> types = [];
        foreach (HitObjectType name in Enum.GetValues(typeof(HitObjectType)))
        {
            if ((data & (int)name) != 0x00000000) types.Add(name);
        }
        if (types.Count != 1) throw new Exception("Invalid hit object type.");

        return types.First();
    }

    /// <summary>
    /// Returns a <see cref="CurveType"/> from a character.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static CurveType ParseCurveType(char c) => c switch
    {
        'C' => CurveType.Catmull,
        'B' => CurveType.Bezier,
        'L' => CurveType.Linear,
        _ => CurveType.PerfectCurve
    };

    /// <summary>
    /// Returns a Vector3 from a string.
    /// </summary>
    /// <param name="vectorString"></param>
    /// <returns></returns>
    public static Vector3 ParseVector3(string vectorString)
    {
        string[] split = vectorString.Split(',');
        return new Vector3(float.Parse(split[0], CultureInfo.InvariantCulture), float.Parse(split[1], CultureInfo.InvariantCulture), float.Parse(split[2], CultureInfo.InvariantCulture));
    }
}
