using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a hit object in a beatmap.
/// </summary>
public class HitObject : IHitObject
{
    private double _timeMilliseconds;

    /// <summary>
    /// Gets or sets the coordinates of the hit object.
    /// </summary>
    public Vector2 Coordinates { get; set; }
    /// <summary>
    /// Gets or sets the time at which the hit object appears in the beatmap.
    /// </summary>
    public TimeSpan Time
    {
        get => TimeSpan.FromMilliseconds(_timeMilliseconds);
        set => _timeMilliseconds = value.TotalMilliseconds;
    }
    /// <summary>
    /// Gets or sets the type of the hit object.
    /// </summary>
    public HitObjectType Type { get; }

    /// <summary>
    /// Gets or sets the hit sample and its hit sounds associated with the hit object.
    /// </summary>
    public (HitSample SampleData, List<HitSound> Sounds) HitSounds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the hit object starts a new combo.
    /// </summary>
    public bool NewCombo { get; set; }

    /// <summary>
    /// Gets or sets the color of the combo associated with the hit object.
    /// </summary>
    public uint ComboOffset { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HitObject"/> class.
    /// </summary>
    /// <param name="coordinates">The coordinates of the hit object.</param>
    /// <param name="time">The time at which the hit object appears in the beatmap.</param>
    /// <param name="type">The type of the hit object.</param>
    /// <param name="hitSounds">The list of hit sounds associated with the hit object.</param>
    /// <param name="newCombo">A value indicating whether the hit object starts a new combo.</param>
    /// <param name="comboOffset">The color of the combo associated with the hit object.</param>
    protected HitObject(Vector2 coordinates, double timeMilliseconds, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboOffset)
    {
        Coordinates = coordinates;
        Type = type;
        _timeMilliseconds = timeMilliseconds;
        HitSounds = hitSounds;
        NewCombo = newCombo;
        ComboOffset = comboOffset;
    }

    protected HitObject(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboOffset)
        : this(coordinates, time.TotalMilliseconds, type, hitSounds, newCombo, comboOffset)
    {
    }

    protected HitObject(HitObject other)
    {
        Coordinates = other.Coordinates;
        Type = other.Type;
        _timeMilliseconds = other._timeMilliseconds;
        HitSounds = other.HitSounds;
        NewCombo = other.NewCombo;
        ComboOffset = other.ComboOffset;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HitObject"/> class.
    /// </summary>
    protected HitObject()
    {
        Coordinates = new();
        Type = HitObjectType.Circle;
        _timeMilliseconds = 0;
        HitSounds = (new HitSample(), []);
        NewCombo = false;
        ComboOffset = 0;
    }

    internal double TimeMilliseconds
    {
        get => _timeMilliseconds;
        set => _timeMilliseconds = value;
    }

    /// <summary>
    /// Parses a hit object from a string.
    /// </summary>
    /// <param name="splitData">The string containing the hit object data, split by commas. The format is as follows:
    /// X,Y,Time,Type,HitSound[,HitSample]</param>
    /// <returns>A <see cref="HitObject"/> instance representing the parsed hit object.</returns>
    public static HitObject Decode(List<string> splitData)
    {
        try
        {
            var hasHitSample = splitData.Last().Contains(':');
            var timeMilliseconds = double.Parse(splitData[2], CultureInfo.InvariantCulture);

            return new HitObject(
                coordinates: new Vector2(float.Parse(splitData[0], CultureInfo.InvariantCulture), float.Parse(splitData[1], CultureInfo.InvariantCulture)),
                timeMilliseconds: timeMilliseconds,
                type: Helper.ParseHitObjectType(int.Parse(splitData[3])),
                hitSounds: !hasHitSample ? (new HitSample(), Helper.ParseHitSounds(int.Parse(splitData[4]))) : (HitSample.Decode(splitData.Last()), Helper.ParseHitSounds(int.Parse(splitData[4]))),
                newCombo: (int.Parse(splitData[3]) & (1 << 2)) != 0,
                comboOffset: (uint)((int.Parse(splitData[3]) & (1 << 4 | 1 << 5 | 1 << 6)) >> 4)
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse HitObject {ex}");
        }
    }

    /// <summary>
    /// Encodes the hit object into a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        builder.Append($"{Helper.FormatCoord(Coordinates.X)},{Helper.FormatCoord(Coordinates.Y)},");
        builder.Append($"{Helper.FormatTime(_timeMilliseconds)},");

        var type = (int)Type;

        if (NewCombo)
        {
            type |= 1 << 2;
        }

        type |= (int)ComboOffset << 4;

        builder.Append($"{type},");
        builder.Append($"{Helper.EncodeHitSounds(HitSounds.Sounds)} ,".Replace(" ", string.Empty));

        builder.Append($"{HitSounds.SampleData.Encode()}");

        return builder.ToString();
    }
}
