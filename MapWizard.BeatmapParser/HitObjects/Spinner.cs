using System.Globalization;
using System.Numerics;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a spinner hit object in a beatmap.
/// </summary>
public class Spinner : HitObject
{
    /// <summary>
    /// Gets or sets the end time of the spinner.
    /// </summary>
    public TimeSpan End { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    /// <param name="coordinates">The coordinates of the spinner.</param>
    /// <param name="time">The time of the spinner.</param>
    /// <param name="type">The type of the spinner.</param>
    /// <param name="hitSounds">The list of hit sounds associated with the spinner.</param>
    /// <param name="newCombo">A value indicating whether the spinner starts a new combo.</param>
    /// <param name="comboColour">The color of the combo associated with the spinner.</param>
    /// <param name="end">The end time of the spinner.</param>
    private Spinner(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour, TimeSpan end)
    : base(coordinates, time, type, hitSounds, newCombo, comboColour)
    {
        End = end;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    public Spinner()
    {
        End = new TimeSpan();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    public Spinner(IHitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.Type, baseObject.HitSounds, baseObject.NewCombo, baseObject.ComboOffset)
    {
        End = new TimeSpan();
    }

    /// <summary>
    /// Converts a list of strings into a <see cref="Spinner"/> object.
    /// </summary>
    /// <param name="splitData"></param>
    /// <returns></returns>
    public new static Spinner Decode(List<string> splitData)
    {
        try
        {
            var hasHitSample = splitData.Last().Contains(":");
            return new Spinner(
                coordinates: new Vector2(float.Parse(splitData[0], CultureInfo.InvariantCulture), float.Parse(splitData[1], CultureInfo.InvariantCulture)),
                time: TimeSpan.FromMilliseconds(double.Parse(splitData[2], CultureInfo.InvariantCulture)),
                type: Helper.ParseHitObjectType(int.Parse(splitData[3])),
                hitSounds: !hasHitSample ? (new HitSample(), Helper.ParseHitSounds(int.Parse(splitData[4]))) : (HitSample.Decode(splitData.Last()), Helper.ParseHitSounds(int.Parse(splitData[4]))),
                newCombo: (int.Parse(splitData[3]) & (1 << 2)) != 0,
                comboColour: (uint)((int.Parse(splitData[3]) & (1 << 4 | 1 << 5 | 1 << 6)) >> 4),
                end: TimeSpan.FromMilliseconds(double.Parse(splitData[5], CultureInfo.InvariantCulture))
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
    /// <returns></returns>
    public new string Encode()
    {
        StringBuilder builder = new();

        builder.Append($"{Coordinates.X},{Coordinates.Y},");
        builder.Append($"{Time.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},");

        var type = (int)Type;

        if (NewCombo)
        {
            type |= 1 << 2;
        }

        type |= (int)ComboOffset << 4;

        builder.Append($"{type},");
        builder.Append($"{Helper.EncodeHitSounds(HitSounds.Sounds)},");

        builder.Append($"{End.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},");

        builder.Append($"{HitSounds.SampleData.Encode()}");

        return builder.ToString();
    }
}