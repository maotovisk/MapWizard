using System.Globalization;
using System.Numerics;
using System.Text;
using MapWizard.BeatmapParser;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a <see cref="Slider"/> hit object.
/// </summary>
public class Slider : HitObject
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
    /// End time of the slider.
    /// This is a calculated property.
    /// </summary>
    public TimeSpan EndTime { get; set; }
    /// <summary>
    /// Hit sound of the slider track.
    /// </summary>
    public (HitSample SampleData, List<HitSound> Sounds) HeadSounds { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<(HitSample SampleData, List<HitSound> Sounds)>? RepeatSounds { get; set; }


    /// <summary>
    /// Hit sound of the slider track.
    /// </summary>
    public (HitSample SampleData, List<HitSound> Sounds) TailSounds { get; set; }

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
    /// <param name="endTime"></param>
    /// <param name="headSounds"></param>
    /// <param name="repeatSounds"></param>
    /// <param name="tailSounds"></param>
    public Slider(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour, CurveType curveType, List<Vector2> curvePoints, uint repeats, double length, TimeSpan endTime, (HitSample SampleData, List<HitSound> Sounds) headSounds, List<(HitSample SampleData, List<HitSound> Sounds)>? repeatSounds, (HitSample SampleData, List<HitSound> Sounds) tailSounds) :
        base(coordinates, time, type, hitSounds, newCombo, comboColour)
    {
        CurveType = curveType;
        CurvePoints = curvePoints;
        EndTime = endTime;
        Repeats = repeats;
        Length = length;
        HeadSounds = headSounds;
        RepeatSounds = repeatSounds;
        TailSounds = tailSounds;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    public Slider()
    {
        CurveType = CurveType.Bezier;
        CurvePoints = [];
        Repeats = 1;
        Length = 0;
        EndTime = TimeSpan.FromSeconds(0);
        HeadSounds = (new HitSample(), []);
        RepeatSounds = [];
        TailSounds = (new HitSample(), []);
        Coordinates = new Vector2();
        Time = TimeSpan.FromSeconds(0);
        NewCombo = false;
        ComboColour = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    /// <param name="baseObject"></param>
    private Slider(IHitObject baseObject) : base(baseObject.Coordinates, baseObject.Time, baseObject.Type, baseObject.HitSounds, baseObject.NewCombo, baseObject.ComboColour)
    {
        CurveType = CurveType.Bezier;
        CurvePoints = [];
        Repeats = 1;
        Length = 0;
        EndTime = Time;
    }

    /// <summary>
    /// Parses a slider hit object from a split hitObject line.
    /// </summary>
    /// <param name="split"></param>
    /// <param name="timingPoints"></param>
    /// <param name="difficulty"></param>
    /// <returns></returns>
    public static Slider Decode(List<string> split, TimingPoints timingPoints, Difficulty difficulty)
    {
        // x,   y,  time,   type,   hitSound,   curveType|curvePoints,  slides, length, edgeSounds,     ,edgeSets   ,hitSample
        // 0    1   2       3       4           5                       6       7          	      8             9           10
        try
        {
            if (split.Count > 11) throw new ArgumentException("Invalid slider hit object line.");

            if (split.Count < 8) throw new ArgumentException("Invalid slider hit object line.");

            List<(HitSample SampleData, List<HitSound> Sounds)> sliderHitSounds = [];

            if (split.Count > 9)
            {
                var edgeHitSounds = split[8].Split("|").Select(sound => Helper.ParseHitSounds(int.Parse(sound)).Distinct().ToList()).ToList();
                var edgeSampleSets = split[9].Split('|').Select(sample => (HitSample)HitSample.Decode(sample)).ToList();
                sliderHitSounds = edgeHitSounds.Zip(edgeSampleSets, (hitSound, sampleSet) => (sampleSet, hitSound)).ToList();
            }

            var objectParams = split[5].Split('|');
            return new Slider(HitObject.Decode(split))
            {
                CurveType = Helper.ParseCurveType(char.Parse(objectParams[0])),
                CurvePoints = objectParams.Skip(1).Select(x =>
                    {
                        var curvePoints = x.Split(':');
                        return new Vector2(float.Parse(curvePoints[0]), float.Parse(curvePoints[1]));
                    }).ToList(),
                Repeats = uint.Parse(split[6] == "NaN" ? "1" : split[6]),
                Length = double.Parse(split[7] == "NaN" ? "0" : split[7], CultureInfo.InvariantCulture),
                EndTime = Helper.CalculateEndTime(
                    sliderMultiplier: difficulty.SliderMultiplier * timingPoints.GetSliderVelocityAt(double.Parse(split[2], CultureInfo.InvariantCulture)),
                    beatLength: timingPoints.GetUninheritedTimingPointAt(double.Parse(split[2], CultureInfo.InvariantCulture))?.BeatLength ?? 500,
                    startTime: TimeSpan.FromMilliseconds(double.Parse(split[2], CultureInfo.InvariantCulture)),
                    pixelLength: double.Parse(split[7] == "NaN" ? "0" : split[7], CultureInfo.InvariantCulture),
                    repeats: int.Parse(split[6] == "NaN" ? "1" : split[6])
                ),
                HeadSounds = sliderHitSounds.Count == 0 ? (new HitSample(), new List<HitSound>()) : sliderHitSounds[0],
                RepeatSounds = sliderHitSounds.Count > 2 ? sliderHitSounds[1..^1] : null,
                TailSounds = sliderHitSounds.Count == 0 ? (new HitSample(), new List<HitSound>()) : sliderHitSounds.Last(),
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse HitObject line '{string.Join(",", split)}' \n{ex}");
        }
    }

    /// <summary>
    /// Encodes the hit object into a string.
    /// </summary>
    /// <returns></returns>
    public new string Encode()
    {
        // x,   y,  time,   type,   hitSound,   curveType|curvePoints,  slides, length, edgeSounds,     ,edgeSets   ,hitSample
        // 0    1   2       3       4           5                       6       7          	      8             9           10
        StringBuilder builder = new();

        builder.Append($"{Coordinates.X},{Coordinates.Y},");
        builder.Append($"{Time.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},");

        int type = (int)Type | (NewCombo ? 1 << 2 : 0) | ((int)ComboColour << 4);

        builder.Append($"{type},");
        builder.Append($"{Helper.EncodeHitSounds(HitSounds.Sounds)},");

        builder.Append($"{Helper.EncodeCurveType(CurveType)}|{string.Join("|", CurvePoints.Select(x => $"{x.X}:{x.Y}"))},");

        builder.Append($"{Repeats},");

        builder.Append($"{Length.ToString(CultureInfo.InvariantCulture)}");

        List<int> edgeSounds = [];

        if (HeadSounds.Sounds.Count > 0)
        {
            edgeSounds.Add(Helper.EncodeHitSounds(HeadSounds.Sounds));
        }

        if (RepeatSounds != null)
        {
            edgeSounds.AddRange(RepeatSounds.Select(x => Helper.EncodeHitSounds(x.Sounds)));
        }

        if (TailSounds.Sounds.Count > 0)
        {
            edgeSounds.Add(Helper.EncodeHitSounds(TailSounds.Sounds));
        }
        if (edgeSounds.Count > 0)
            builder.Append($",{string.Join("|", edgeSounds)}");

        List<string> edgeSets = [];

        if (HeadSounds.SampleData.FileName != string.Empty)
        {
            edgeSets.Add(HeadSounds.SampleData.Encode());
        }

        if (RepeatSounds != null)
        {
            edgeSets.AddRange(RepeatSounds.Select(x => x.SampleData.Encode()));
        }

        if (TailSounds.SampleData.FileName != string.Empty)
        {
            edgeSets.Add(TailSounds.SampleData.Encode());
        }

        if (edgeSets.Count > 0)
            builder.Append($",{string.Join("|", edgeSets)}");

        if (edgeSets.Count > 0 && edgeSounds.Count > 0)
            builder.Append($",{HitSounds.SampleData.Encode()}");

        return builder.ToString();
    }
}