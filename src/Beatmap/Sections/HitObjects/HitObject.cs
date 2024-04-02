using System.Globalization;
using System.Numerics;

namespace BeatmapParser;

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
    /// Gets or sets the type of the hit object.
    /// </summary>
    public HitObjectType Type { get; set; }

    /// <summary>
    /// Gets or sets the hit sample and its hitsounds associated with the hit object.
    /// </summary>
    public (IHitSample SampleData, List<HitSound> Sounds) HitSounds { get; set; }

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
    /// <param name="type">The type of the hit object.</param>
    /// <param name="hitSounds">The list of hit sounds associated with the hit object.</param>
    /// <param name="newCombo">A value indicating whether the hit object starts a new combo.</param>
    /// <param name="comboColour">The color of the combo associated with the hit object.</param>
    public HitObject(Vector2 coordinates, TimeSpan time, HitObjectType type, (IHitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour)
    {
        Coordinates = coordinates;
        Type = type;
        Time = time;
        HitSounds = hitSounds;
        NewCombo = newCombo;
        ComboColour = comboColour;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HitObject"/> class.
    /// </summary>
    public HitObject()
    {
        Coordinates = new();
        Type = HitObjectType.Circle;
        Time = TimeSpan.FromSeconds(0);
        HitSounds = (new HitSample(), new List<HitSound>());
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Parses a hit object from a string.
    /// </summary>
    /// <param name="splitData"></param>
    /// <returns></returns>
    public static HitObject Decode(List<string> splitData)
    {
        // x,   y,  time,   type, hitSound, objectParams, hitSample
        // 0    1   2       3     4         5            6
        try
        {
            bool hasHitSample = splitData.Last().Contains(":");
            return new HitObject(
                coordinates: new Vector2(float.Parse(splitData[0], CultureInfo.InvariantCulture), float.Parse(splitData[1], CultureInfo.InvariantCulture)),
                time: TimeSpan.FromMilliseconds(double.Parse(splitData[2], CultureInfo.InvariantCulture)),
                type: Helper.ParseHitObjectType(int.Parse(splitData[3])),
                hitSounds: !hasHitSample ? (new HitSample(), Helper.ParseHitSounds(int.Parse(splitData[4]))) : (HitSample.Decode(splitData.Last()), Helper.ParseHitSounds(int.Parse(splitData[4]))),
                newCombo: (int.Parse(splitData[3]) & (1 << 2)) != 0,
                comboColour: (uint)((int.Parse(splitData[3]) & (1 << 4 | 1 << 5 | 1 << 6)) >> 4)
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse HitObject {ex}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsCircle<T>(T obj) => obj != null && obj.GetType().GetInterfaces().Contains(typeof(ICircle));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsManiaHold<T>(T obj) => obj != null && obj.GetType().GetInterfaces().Contains(typeof(IManiaHold));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsSlider<T>(T obj) => obj != null && obj.GetType().GetInterfaces().Contains(typeof(ISlider));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsSpinner<T>(T obj) => obj != null && obj.GetType().GetInterfaces().Contains(typeof(ISpinner));

}
