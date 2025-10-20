using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a spinner hit object in a beatmap.
/// </summary>
public class Spinner : HitObject
{
    private double _endMilliseconds;

    /// <summary>
    /// Gets or sets the end time of the spinner.
    /// </summary>
    public TimeSpan End
    {
        get => TimeSpan.FromMilliseconds(_endMilliseconds);
        set => _endMilliseconds = value.TotalMilliseconds;
    }

    internal double EndMilliseconds
    {
        get => _endMilliseconds;
        set => _endMilliseconds = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    /// <param name="coordinates">The coordinates of the spinner.</param>
    /// <param name="time">The time of the spinner.</param>
    /// <param name="type">The type of the spinner.</param>
    /// <param name="hitSounds">The list of hit sounds associated with the spinner.</param>
    /// <param name="newCombo">A value indicating whether the spinner starts a new combo.</param>
    /// <param name="comboOffset">The color of the combo associated with the spinner.</param>
    /// <param name="end">The end time of the spinner.</param>
    private Spinner(Vector2 coordinates, double timeMilliseconds, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboOffset, double endMilliseconds)
        : base(coordinates, timeMilliseconds, type, hitSounds, newCombo, comboOffset)
    {
        _endMilliseconds = endMilliseconds;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    public Spinner()
    {
        _endMilliseconds = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    public Spinner(HitObject baseObject) : base(baseObject)
    {
        _endMilliseconds = 0;
    }

    /// <summary>
    /// Converts a list of strings into a <see cref="Spinner"/> object.
    /// </summary>
    /// <param name="splitData">The list of strings containing the spinner data, split by commas.</param>
    /// <returns>A <see cref="Spinner"/> instance representing the parsed spinner.</returns>
    public new static Spinner Decode(List<string> splitData)
    {
        try
        {
            var hasHitSample = splitData.Last().Contains(":");
            var timeMilliseconds = double.Parse(splitData[2], CultureInfo.InvariantCulture);
            var endMilliseconds = double.Parse(splitData[5], CultureInfo.InvariantCulture);

            return new Spinner(
                coordinates: new Vector2(float.Parse(splitData[0], CultureInfo.InvariantCulture), float.Parse(splitData[1], CultureInfo.InvariantCulture)),
                timeMilliseconds: timeMilliseconds,
                type: Helper.ParseHitObjectType(int.Parse(splitData[3])),
                hitSounds: !hasHitSample ? (new HitSample(), Helper.ParseHitSounds(int.Parse(splitData[4]))) : (HitSample.Decode(splitData.Last()), Helper.ParseHitSounds(int.Parse(splitData[4]))),
                newCombo: (int.Parse(splitData[3]) & (1 << 2)) != 0,
                comboOffset: (uint)((int.Parse(splitData[3]) & (1 << 4 | 1 << 5 | 1 << 6)) >> 4),
                endMilliseconds: endMilliseconds
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse HitObject {ex}");
        }
    }

    /// <summary>
    /// Encodes the spinner hit object into a string.
    /// </summary>
    /// <returns>A string representation of the spinner hit object.</returns>
    public new string Encode()
    {
        StringBuilder builder = new();

        builder.Append($"{Helper.FormatCoord(Coordinates.X)},{Helper.FormatCoord(Coordinates.Y)},");
        builder.Append($"{Helper.FormatTime(TimeMilliseconds)},");

        var type = (int)Type;

        if (NewCombo)
        {
            type |= 1 << 2;
        }

        type |= (int)ComboOffset << 4;

        builder.Append($"{type},");
        builder.Append($"{Helper.EncodeHitSounds(HitSounds.Sounds)},");

        builder.Append($"{Helper.FormatTime(_endMilliseconds)},");

        builder.Append($"{HitSounds.SampleData.Encode()}");

        return builder.ToString();
    }
}
