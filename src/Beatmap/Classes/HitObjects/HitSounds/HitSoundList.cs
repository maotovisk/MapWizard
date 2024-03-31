namespace Beatmap;

/// <summary>
/// Hitsounds used in a beatmap.
/// </summary>
public class HitSoundList
{
    /// <summary>
    /// Converts integer bitwise into a hitsound list.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<HitSound> FromData(int data)
    {
        List<HitSound> hitSounds = [];
        foreach (HitSound name in Enum.GetValues(typeof(HitSound)))
        {
            if ((data & (int)name) != 0x000000000) hitSounds.Add(name);
        }
        return hitSounds;
    }
}