using System.Numerics;

namespace Beatmap;

/// <summary>
/// Represents a <see cref="Slider"/> hit object.
/// </summary>
public class Slider : HitObject, ISlider
{
    /// <summary>
    /// Curve type of the slider.
    /// </summary>
    public CurveType CurveType { get; set; }

    /// <summary>
    /// Points of the slider path.
    /// </summary>
    public List<Vector2> CurvePoints { get; set; }

    /// <summary>
    /// Number of repeats of the slider.
    /// </summary>
    public uint Repeats { get; set; }

    /// <summary>
    /// Length of the slider.
    /// </summary>
    public uint Length { get; set; }

    /// <summary>
    /// Hit sound of the slider track.
    /// </summary>
    public List<HitSound> EdgeHitSound { get; set; }

    /// <summary>
    /// Sample sets of the slider track.
    /// </summary>
    public SampleSet EdgeSampleSets { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="time"></param>
    /// <param name="hitSounds"></param>
    /// <param name="hitSample"></param>
    /// <param name="newCombo"></param>
    /// <param name="comboColour"></param>
    /// <param name="curveType"></param>
    /// <param name="curvePoints"></param>
    /// <param name="repeats"></param>
    /// <param name="length"></param>
    /// <param name="edgeHitSounds"></param>
    /// <param name="edgeSampleSets"></param>
    public Slider(Vector2 coordinates, TimeSpan time, List<HitSound> hitSounds, IHitSample hitSample, bool newCombo, uint comboColour, CurveType curveType, List<Vector2> curvePoints, uint repeats, uint length, List<HitSound> edgeHitSounds, SampleSet edgeSampleSets) :
        base(coordinates, time, hitSounds, hitSample, newCombo, comboColour)
    {
        CurveType = curveType;
        CurvePoints = curvePoints;
        Repeats = repeats;
        Length = length;
        EdgeHitSound = edgeHitSounds;
        EdgeSampleSets = edgeSampleSets;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    public Slider() : base()
    {
        CurveType = CurveType.Bezier;
        CurvePoints = new List<Vector2>();
        Repeats = 1;
        Length = 0;
        EdgeHitSound = [HitSound.Normal];
        EdgeSampleSets = SampleSet.Default;
        Coordinates = new Vector2();
        Time = TimeSpan.FromSeconds(0);
        HitSounds = new List<HitSound>();
        HitSampleData = new HitSample();
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    public Slider(HitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.HitSounds, baseObject.HitSampleData, baseObject.NewCombo, baseObject.ComboColour)
    {
        CurveType = CurveType.Bezier;
        CurvePoints = new List<Vector2>();
        Repeats = 1;
        Length = 0;
        EdgeHitSound = [HitSound.Normal];
        EdgeSampleSets = SampleSet.Default;
    }

    /// <summary>
    /// Parses a slider hit object from a splitted hitObject line.
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    public static Slider ParseFromData(List<string> split)
    {
        // x,   y,  time,   type,   hitSound,   curveType|curvePoints,  slides, length, edgeSounds  ,edgeSets   ,hitSample
        // 0    1   2       3       4           5                       6       7       8           9           10

        var objectParams = split[5].Split('|');

        var slider = new Slider(HitObject.FromData(split))
        {
            CurveType = (CurveType)Enum.Parse(typeof(CurveType), objectParams[0]),
            CurvePoints = objectParams[1..^1].Select(x =>
                {
                    var split = x.Split(':');
                    return new Vector2(int.Parse(split[0]), int.Parse(split[1]));
                }).ToList(),
            Repeats = uint.Parse(split[6]),
            Length = uint.Parse(split[7]),
            EdgeHitSound = HitSoundList.FromData(int.Parse(split[8])),
            EdgeSampleSets = (SampleSet)Enum.Parse(typeof(SampleSet), split[9]),
        };
        return slider;
    }
}