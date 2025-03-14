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
    public uint Slides { get; set; }

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
    /// <param name="slides"></param>
    /// <param name="length"></param>
    /// <param name="endTime"></param>
    /// <param name="headSounds"></param>
    /// <param name="repeatSounds"></param>
    /// <param name="tailSounds"></param>
    public Slider(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboColour, CurveType curveType, List<Vector2> curvePoints, uint slides, double length, TimeSpan endTime, (HitSample SampleData, List<HitSound> Sounds) headSounds, List<(HitSample SampleData, List<HitSound> Sounds)>? repeatSounds, (HitSample SampleData, List<HitSound> Sounds) tailSounds) :
        base(coordinates, time, type, hitSounds, newCombo, comboColour)
    {
        CurveType = curveType;
        CurvePoints = curvePoints;
        EndTime = endTime;
        Slides = slides;
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
        Slides = 1;
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
        Slides = 1;
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
            var baseObject = HitObject.Decode(split);

            // slider shorthand, should apply the base object hitsounds for all edges
            if (baseObject.HitSounds.Sounds.Count != 0 && split.Count == 8)
            {
                sliderHitSounds = Enumerable.Repeat(baseObject.HitSounds, (int)uint.Parse(split[6]) + 1).ToList();
            }
            
            return new Slider(baseObject)
            {
                CurveType = Helper.ParseCurveType(char.Parse(objectParams[0])),
                CurvePoints = objectParams.Skip(1).Select(x =>
                {
                    var curvePoints = x.Split(':');
                    return new Vector2(float.Parse(curvePoints[0]), float.Parse(curvePoints[1]));
                }).ToList(),
                Slides = uint.Parse(split[6] == "NaN" ? "1" : split[6]),
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
        // Format:
        // x,y,time,type,hitSound,curveType|curvePoints,slides,length,edgeSounds,edgeSets,hitSample
        // Indexes:  0  1   2      3        4           5                  6      7          8         9         10
        StringBuilder builder = new();
        
        builder.Append($"{Coordinates.X},{Coordinates.Y},");
        builder.Append($"{Time.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},");

        int type = (int)Type | (NewCombo ? 1 << 2 : 0) | ((int)ComboColour << 4);
        builder.Append($"{type},");

        // HitSound field
        builder.Append($"{Helper.EncodeHitSounds(HitSounds.Sounds)},");

        builder.Append($"{Helper.EncodeCurveType(CurveType)}|{string.Join("|", CurvePoints.Select(p => $"{p.X}:{p.Y}"))},");

        builder.Append($"{Slides},");
        builder.Append($"{Length.ToString(CultureInfo.InvariantCulture)}");

        // this is used to know if we are going to shorthand the slider
        bool allSoundsMatch =
            (Slides > 1 && RepeatSounds != null &&
             RepeatSounds.All(x => x.Sounds.SequenceEqual(HitSounds.Sounds) &&
                                    x.SampleData.Equals(HitSounds.SampleData)) &&
             TailSounds.Sounds.SequenceEqual(HitSounds.Sounds) &&
             TailSounds.SampleData.Equals(HitSounds.SampleData) &&
             HeadSounds.Sounds.SequenceEqual(HitSounds.Sounds) &&
             HeadSounds.SampleData.Equals(HitSounds.SampleData)) ||
            (Slides == 1 &&
             TailSounds.Sounds.SequenceEqual(HitSounds.Sounds) &&
             TailSounds.SampleData.Equals(HitSounds.SampleData) &&
             HeadSounds.Sounds.SequenceEqual(HitSounds.Sounds) &&
             HeadSounds.SampleData.Equals(HitSounds.SampleData));

        bool hasNonDefaultSampleData = new[] { HitSounds.SampleData, HeadSounds.SampleData, TailSounds.SampleData }
            .Concat(RepeatSounds?.Select(x => x.SampleData) ?? Enumerable.Empty<HitSample>())
            .Any(HasNonDefaultSampleData);

        bool hasNonDefaultAdditions = new[] { HitSounds.Sounds, HeadSounds.Sounds, TailSounds.Sounds }
            .Concat(RepeatSounds?.Select(x => x.Sounds) ?? Enumerable.Empty<List<HitSound>>())
            .Any(sounds => sounds.Count > 0 && !sounds.Contains(HitSound.None));

        bool shouldShorthand = allSoundsMatch || (!hasNonDefaultSampleData && !hasNonDefaultAdditions);
        if (shouldShorthand)
        {
            return builder.ToString();
        }

        List<int> edgeSounds = new();
        if (HeadSounds.Sounds.Count > 0 ||
            (RepeatSounds != null && RepeatSounds.Any(x => x.Sounds.Count > 0)) ||
            TailSounds.Sounds.Count > 0)
        {
            edgeSounds.Add(Helper.EncodeHitSounds(HeadSounds.Sounds));
        }

        if (RepeatSounds != null && RepeatSounds.Any(x => x.Sounds.Count > 0) ||
            TailSounds.Sounds.Count > 0)
        {
            edgeSounds.AddRange(RepeatSounds?.Select(x => Helper.EncodeHitSounds(x.Sounds))
                              ?? Enumerable.Repeat(0, (int)Slides - 1));
        }

        if (TailSounds.Sounds.Count > 0)
        {
            edgeSounds.Add(Helper.EncodeHitSounds(TailSounds.Sounds));
        }

        if (edgeSounds.Count > 0)
        {
            builder.Append($",{string.Join("|", edgeSounds)}");
        }

        List<string> edgeSets = new()
        {
            HeadSounds.SampleData.Encode()
        };

        if (RepeatSounds != null)
        {
            edgeSets.AddRange(RepeatSounds.Select(x => x.SampleData.Encode()));
        }
        
        
        // IMPORTANT: For the tail sample, if it equals the base hit sample then we output default "0:0" 
        // rather than repeating the head’s value.
        string tailEdgeSet = TailSounds.SampleData.Equals(HitSounds.SampleData)
            ? "0:0"
            : TailSounds.SampleData.Encode();
        edgeSets.Add(tailEdgeSet);

        if (edgeSets.Count > 0)
        {
            var processedEdgeSets = edgeSets.Select(s =>
            {
                var parts = s.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : s;
            });
            builder.Append($",{string.Join("|", processedEdgeSets)}");
        }

        // Append the overall hit sample field (with the full encoding, including trailing colon).
        if (edgeSets.Count > 0 || edgeSounds.Count > 0 || HasNonDefaultSampleData(HitSounds.SampleData))
        {
            builder.Append($",{HitSounds.SampleData.Encode()}");
        }
        
        return builder.ToString();
    }

    /// <summary>
    /// Returns true if the given hit sample has non-default values.
    /// </summary>
    private static bool HasNonDefaultSampleData(HitSample sample) =>
        sample.NormalSet != SampleSet.Default ||
        sample.AdditionSet != SampleSet.Default ||
        sample.Index != 0 ||
        sample.Volume != 0;
}