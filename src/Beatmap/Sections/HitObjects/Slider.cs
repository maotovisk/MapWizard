using System.Numerics;

namespace BeatmapParser;

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
    public double Length { get; set; }
    /// <summary>
    /// Hit sound of the slider track.
    /// </summary>
    public (IHitSample SampleData, List<HitSound> Sounds) HeadSounds { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<(IHitSample SampleData, List<HitSound> Sounds)>? RepeatSounds { get; set; }


    /// <summary>
    /// Hit sound of the slider track.
    /// </summary>
    public (IHitSample SampleData, List<HitSound> Sounds) TailSounds { get; set; }

    ///  <summary>
    /// 
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="time"></param>
    /// <param name="type"></param>
    /// <param name="hitSounds"></param>
    /// <param name="newCombo"></param>
    /// <param name="comboColour"></param>
    /// <param name="curveType"></param>
    /// <param name="curvePoints"></param>
    /// <param name="repeats"></param>
    /// <param name="length"></param>
    /// <param name="headSounds"></param>
    /// <param name="repeatSounds"></param>
    /// <param name="tailSounds"></param>
    public Slider(Vector2 coordinates, TimeSpan time, HitObjectType type, (IHitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour, CurveType curveType, List<Vector2> curvePoints, uint repeats, double length, (IHitSample SampleData, List<HitSound> Sounds) headSounds, List<(IHitSample SampleData, List<HitSound> Sounds)>? repeatSounds, (IHitSample SampleData, List<HitSound> Sounds) tailSounds) :
        base(coordinates, time, type, hitSounds, newCombo, comboColour)
    {
        CurveType = curveType;
        CurvePoints = curvePoints;
        Repeats = repeats;
        Length = length;
        HeadSounds = headSounds;
        RepeatSounds = repeatSounds;
        TailSounds = tailSounds;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    public Slider() : base()
    {
        CurveType = CurveType.Bezier;
        CurvePoints = [];
        Repeats = 1;
        Length = 0;
        HeadSounds = (new HitSample(), new List<HitSound>());
        RepeatSounds = [];
        TailSounds = (new HitSample(), new List<HitSound>());
        Coordinates = new Vector2();
        Time = TimeSpan.FromSeconds(0);
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    public Slider(HitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.Type, baseObject.HitSounds, baseObject.NewCombo, baseObject.ComboColour)
    {
        CurveType = CurveType.Bezier;
        CurvePoints = [];
        Repeats = 1;
        Length = 0;

    }

    /// <summary>
    /// Parses a slider hit object from a splitted hitObject line.
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    public static new Slider Decode(List<string> split)
    {
        // x,   y,  time,   type,   hitSound,   curveType|curvePoints,  slides, length, edgeSounds,     ,edgeSets   ,hitSample
        // 0    1   2       3       4           5                       6       7          	      8             9           10
        try
        {
            if (split.Count > 11) throw new ArgumentException("Invalid slider hit object line.");

            if (split.Count < 8) throw new ArgumentException("Invalid slider hit object line.");
            List<List<HitSound>> EdgeHitSounds = [];
            List<IHitSample> EdgeSampleSets = [];
            List<(IHitSample SampleData, List<HitSound> Sounds)> sliderHitSounds = [];

            if (split.Count > 9)
            {
                EdgeHitSounds = split[8].Split("|").Select(sound => Helper.ParseHitSounds(int.Parse(sound))).ToList();
                EdgeSampleSets = split[9].Split('|').Select(sample => (IHitSample)HitSample.Decode(sample)).ToList();
                sliderHitSounds = EdgeSampleSets.Zip(EdgeHitSounds, (sample, hitSounds) => (sample, hitSounds)).ToList();
            }

            var objectParams = split[5].Split('|');
            return new Slider(HitObject.Decode(split))
            {
                CurveType = Helper.ParseCurveType(char.Parse(objectParams[0])),
                CurvePoints = objectParams.Skip(1).Select(x =>
                    {
                        var split = x.Split(':');
                        return new Vector2(float.Parse(split[0]), float.Parse(split[1]));
                    }).ToList(),
                Repeats = uint.Parse(split[6]),
                Length = double.Parse(split[7]),

                HeadSounds = sliderHitSounds.Count == 0 ? (new HitSample(), new List<HitSound>()) : sliderHitSounds.First(),
                RepeatSounds = sliderHitSounds.Count > 2 ? sliderHitSounds[1..^1] : null,
                TailSounds = sliderHitSounds.Count == 0 ? (new HitSample(), new List<HitSound>()) : sliderHitSounds.Last(),
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse HitObject line '{string.Join(",", split)}' \n{ex}");
        }
    }
}