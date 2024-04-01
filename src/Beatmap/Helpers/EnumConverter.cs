namespace BeatmapParser;

/// <summary>
/// Class to convert bitwise into enum lists.
/// </summary>
public static class EnumConverter
{
    /// <summary>
    /// Converts integer bitwise into a <see cref="HitSound"/> list.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<HitSound> Hitsounds(int data)
    {
        List<HitSound> hitSounds = [];
        foreach (HitSound name in Enum.GetValues(typeof(HitSound)))
        {
            if ((data & (int)name) != 0x000000000) hitSounds.Add(name);
        }
        return hitSounds;
    }

    /// <summary>
    /// Get the a list of effects from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<Effect> Effects(int data)
    {
        List<Effect> types = [];
        foreach (Effect name in Enum.GetValues(typeof(Effect)))
        {
            if ((data & (int)name) != 0x000000000) types.Add(name);
        }
        return types;
    }

    /// <summary>
    /// Gets the hit object type from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static HitObjectType? HitObjectType(int data)
    {
        List<HitObjectType> types = [];
        foreach (HitObjectType name in Enum.GetValues(typeof(HitObjectType)))
        {
            if ((data & (int)name) != 0x000000000) types.Add(name);
        }
        if (types.Count == 1) return types.First();

        return null;
    }
}
