using System.Globalization;
using System.Numerics;

namespace MapWizard.BeatmapParser;

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
        if (data == 0) return [HitSound.None];
        List<HitSound> hitSounds = [];

        foreach (HitSound name in Enum.GetValues(typeof(HitSound)))
        {
            if ((data & (int)name) != 0) hitSounds.Add(name);
        }

        return hitSounds;
    }

    /// <summary>
    /// Encodes a list of hit sounds into a bitwise.
    /// </summary>
    /// <param name="hitSounds"></param>
    /// <returns></returns>
    public static int EncodeHitSounds(List<HitSound> hitSounds)
    {
        var result = 0;
        foreach (var hitSound in hitSounds.Distinct())
        {
            result |= (int)hitSound;
        }
        return result;
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
    /// Encodes a list of effects into a bitwise.
    /// </summary>
    /// <param name="effects"></param>
    /// <returns></returns>
    public static int EncodeEffects(List<Effect> effects)
    {
        var result = 0;
        foreach (Effect effect in effects)
        {
            result |= (int)effect;
        }
        return result;
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

        return types[0];
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
    /// Encodes a <see cref="CurveType"/> into a string.
    /// </summary>
    /// <param name="curveType"></param>
    /// <returns></returns>

    public static string EncodeCurveType(CurveType curveType) => curveType switch
    {
        CurveType.Catmull => "C",
        CurveType.Bezier => "B",
        CurveType.Linear => "L",
        _ => "P"
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
