using System.Numerics;

namespace Beatmap;

/// <summary>
/// Represents a hit object in a beatmap.
/// </summary>
public class HitObject : IHitObject
{
    /// <summary>
    /// Gets or sets the coordinates of the hit object.
    /// </summary>
    public Vector2 Coordinates { get; set; }
    /// <summary>
    /// Gets or sets the time at which the hit object appears in the beatmap.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Gets or sets the list of hit sounds associated with the hit object.
    /// </summary>
    public List<HitSound> HitSounds { get; set; }

    /// <summary>
    /// Gets or sets the hit sample associated with the hit object.
    /// </summary>
    public IHitSample HitSampleData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the hit object starts a new combo.
    /// </summary>
    public bool NewCombo { get; set; }

    /// <summary>
    /// Gets or sets the color of the combo associated with the hit object.
    /// </summary>
    public uint ComboColour { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HitObject"/> class.
    /// </summary>
    /// <param name="coordinates">The coordinates of the hit object.</param>
    /// <param name="time">The time at which the hit object appears in the beatmap.</param>
    /// <param name="hitSounds">The list of hit sounds associated with the hit object.</param>
    /// <param name="hitSample">The hit sample associated with the hit object.</param>
    /// <param name="newCombo">A value indicating whether the hit object starts a new combo.</param>
    /// <param name="comboColour">The color of the combo associated with the hit object.</param>
    public HitObject(Vector2 coordinates, TimeSpan time, List<HitSound> hitSounds, IHitSample hitSample, bool newCombo, uint comboColour)
    {
        Coordinates = coordinates;
        Time = time;
        HitSounds = hitSounds;
        HitSampleData = hitSample;
        NewCombo = newCombo;
        ComboColour = comboColour;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HitObject"/> class.
    /// </summary>
    public HitObject()
    {
        Coordinates = new();
        Time = TimeSpan.FromSeconds(0);
        HitSounds = new List<HitSound>();
        HitSampleData = new HitSample();
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Parses a hit object from a string.
    /// </summary>
    /// <param name="splitData"></param>
    /// <returns></returns>
    public static HitObject FromData(List<string> splitData)
    {
        try
        {
            return new HitObject(
                coordinates: new Vector2(float.Parse(splitData[0]), float.Parse(splitData[1])),
                time: TimeSpan.FromMilliseconds(double.Parse(splitData[2])),
                hitSounds: HitSoundList.FromData(int.Parse(splitData[3])),
                hitSample: HitSample.FromData(splitData[5]),
                newCombo: (int.Parse(splitData[3]) & 0x000000F00) != 0x000000000,
                comboColour: (uint)((int.Parse(splitData[3]) & 0x00FFF0000) >> 4 * 4)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse HitObject {ex}");
            return new HitObject();
        }
    }
}
