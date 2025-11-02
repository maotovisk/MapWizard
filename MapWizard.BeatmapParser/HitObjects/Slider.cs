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
    /// Legacy single curve type (used only for v14 encode/decode).
    /// </summary>
    public CurveType? LegacyCurveType { get; set; }

    /// <summary>
    /// Control points of the slider path. Points may optionally start a new segment (Type != null),
    /// with optional Degree for B-spline.
    /// </summary>
    public List<CurvePoint> ControlPoints { get; set; }

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
    /// <param name="comboOffset"></param>
    /// <param name="controlPoints"></param>
    /// <param name="slides"></param>
    /// <param name="length"></param>
    /// <param name="endTime"></param>
    /// <param name="headSounds"></param>
    /// <param name="repeatSounds"></param>
    /// <param name="tailSounds"></param>
    public Slider(Vector2 coordinates, TimeSpan time, HitObjectType type, (HitSample, List<HitSound>) hitSounds, bool newCombo, uint comboOffset, List<CurvePoint> controlPoints, uint slides, double length, TimeSpan endTime, (HitSample SampleData, List<HitSound> Sounds) headSounds, List<(HitSample SampleData, List<HitSound> Sounds)>? repeatSounds, (HitSample SampleData, List<HitSound> Sounds) tailSounds) :
        base(coordinates, time, type, hitSounds, newCombo, comboOffset)
    {
        ControlPoints = controlPoints;
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
        LegacyCurveType = null;
        ControlPoints = [];
        Slides = 1;
        Length = 0;
        EndTime = TimeSpan.FromSeconds(0);
        HeadSounds = (new HitSample(), []);
        RepeatSounds = [];
        TailSounds = (new HitSample(), []);
        Coordinates = new Vector2();
        Time = TimeSpan.FromSeconds(0);
        NewCombo = false;
        ComboOffset = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    /// <param name="baseObject">The base hit object to copy properties from.</param>
    private Slider(HitObject baseObject) : base(baseObject)
    {
        LegacyCurveType = null;
        ControlPoints = [];
        Slides = 1;
        Length = 0;
        EndTime = Time;
    }

    private static (CurveType type, int? degree) ParseTypeToken(string token)
    {
        // Supports: C, L, P, B, B<degree>
        if (string.IsNullOrEmpty(token)) throw new ArgumentException("Empty token");
        char c = token[0];
        switch (c)
        {
            case 'C': return (CurveType.Catmull, null);
            case 'L': return (CurveType.Linear, null);
            case 'P': return (CurveType.PerfectCurve, null);
            case 'B':
                if (token.Length > 1)
                {
                    if (int.TryParse(token[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var deg) && deg > 0)
                        return (CurveType.BSpline, deg);
                }
                // Bare 'B' -> Bezier
                return (CurveType.Bezier, null);
            default:
                // Unknown letter: default to Bezier for safety
                return (CurveType.Bezier, null);
        }
    }

    private static string EncodeTypeToken(CurveType type, int? degree)
    {
        return type switch
        {
            CurveType.Catmull => "C",
            CurveType.Linear => "L",
            CurveType.PerfectCurve => "P",
            CurveType.Bezier => "B",
            CurveType.BSpline => degree.HasValue && degree.Value > 0 ? $"B{degree.Value}" : "B",
            _ => "B"
        };
    }

    /// <summary>
    /// Parses a slider hit object from a split hitObject line.
    /// </summary>
    /// <param name="split">The split hit object line.</param>
    /// <param name="timingPoints">The timing points.</param>
    /// <param name="difficulty">The difficulty.</param>
    /// <returns>The parsed slider hit object.</returns>
    public static Slider Decode(List<string> split, TimingPoints timingPoints, Difficulty difficulty)
    {
        try
        {
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

            if (baseObject.HitSounds.Sounds.Count != 0 && split.Count == 8)
            {
                sliderHitSounds = Enumerable.Repeat(baseObject.HitSounds, (int)uint.Parse(split[6]) + 1).ToList();
            }
            
            List<CurvePoint> controlPoints = new();
            CurveType? legacyCurveType = null;
            (CurveType type, int? degree)? currentSegmentType = null;

            int startIndex = 0;
            if (objectParams.Length > 0 && !string.IsNullOrEmpty(objectParams[0]) && char.IsLetter(objectParams[0][0]))
            {
                // v14: first token is always a legacy curve type.
                if (Helper.FormatVersion == 14)
                {
                    var (type, degree) = ParseTypeToken(objectParams[0]);
                    legacyCurveType = type;
                    currentSegmentType = (type, degree);
                    startIndex = 1;
                }
                else
                {
                    // v128+: only populate legacyCurveType when there's an isolated type token at the beginning,
                    // e.g. "L|L|..." or "P|L|...". That means the next token also starts with a letter.
                    if (objectParams.Length > 1 && !string.IsNullOrEmpty(objectParams[1]) && char.IsLetter(objectParams[1][0]))
                    {
                        var (type, degree) = ParseTypeToken(objectParams[0]);
                        legacyCurveType = type;
                        currentSegmentType = (type, degree);
                        startIndex = 1;
                    }
                }
            }

            for (int i = startIndex; i < objectParams.Length; i++)
            {
                var token = objectParams[i];
                if (string.IsNullOrEmpty(token)) continue;

                if (char.IsLetter(token[0]))
                {
                    currentSegmentType = ParseTypeToken(token);
                    continue;
                }

                var parts = token.Split(':');
                if (parts.Length < 2) continue;

                if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                    !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    continue;
                }

                var pos = new Vector2(x, y);

                if (currentSegmentType.HasValue)
                {
                    controlPoints.Add(new CurvePoint(pos, currentSegmentType.Value.type, currentSegmentType.Value.degree));
                    currentSegmentType = null;
                }
                else
                {
                    controlPoints.Add(new CurvePoint(pos));
                }
            }
            
            return new Slider(baseObject)
            {
                LegacyCurveType = legacyCurveType,
                ControlPoints = controlPoints,
                Slides = uint.Parse(split[6] == "NaN" ? "1" : split[6]),
                Length = double.Parse(split[7] == "NaN" ? "0" : split[7], CultureInfo.InvariantCulture),
                EndTime = Helper.CalculateEndTime(
                    sliderMultiplier: difficulty.SliderMultiplier * timingPoints.GetSliderVelocityAt(baseObject.TimeMilliseconds),
                    beatLength: timingPoints.GetUninheritedTimingPointAt(baseObject.TimeMilliseconds)?.BeatLength ?? 500,
                    startTime: baseObject.Time,
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
    /// <returns>A string containing the encoded hit object line.</returns>
    public new string Encode()
    {
        // Format:
        // x,y,time,type,hitSound,curveType|curvePoints,slides,length,edgeSounds,edgeSets,hitSample
        // Indexes:  0  1   2      3        4           5                  6      7          8         9         10
        StringBuilder builder = new();
        
        builder.Append($"{Helper.FormatCoord(Coordinates.X)},{Helper.FormatCoord(Coordinates.Y)},");
        builder.Append($"{Helper.FormatTime(TimeMilliseconds)},");

        int type = (int)Type | (NewCombo ? 1 << 2 : 0) | ((int)ComboOffset << 4);
        builder.Append($"{type},");

        // HitSound field
        builder.Append($"{Helper.EncodeHitSounds(HitSounds.Sounds)},");

        // Encode path
        var pathBuilder = new StringBuilder();
        if (Helper.FormatVersion == 14)
        {
            // v14 only supports a single type at the path start
            var legacy = LegacyCurveType ?? inferLegacyTypeFromSegments();
            pathBuilder.Append(Helper.EncodeCurveType(legacy));
            foreach (var cp in ControlPoints)
            {
                pathBuilder.Append('|');
                pathBuilder.Append($"{Helper.FormatCoord(cp.Position.X)}:{Helper.FormatCoord(cp.Position.Y)}");
            }
        }
        else
        {
            // v128: support per-segment types and BSpline degree
            bool first = true;
            
            //check if legacy type is set and first type are actually different
            if (LegacyCurveType.HasValue)
            {
                pathBuilder.Append(Helper.EncodeCurveType(LegacyCurveType.Value));
                pathBuilder.Append('|');
            }
            
            foreach (var cp in ControlPoints)
            {
                if (cp.Type.HasValue)
                {
                    if (!first) pathBuilder.Append('|');
                    pathBuilder.Append(EncodeTypeToken(cp.Type.Value, cp.Degree));
                }
                pathBuilder.Append('|');
                pathBuilder.Append($"{Helper.FormatCoord(cp.Position.X)}:{Helper.FormatCoord(cp.Position.Y)}");
                first = false;
            }
            // If no type token was present at all, prepend a legacy token for compatibility
            if (!ControlPoints.Any(p => p.Type.HasValue))
            {
                pathBuilder.Insert(0, Helper.EncodeCurveType(LegacyCurveType ?? CurveType.Bezier) + "|");
            }
        }

        builder.Append(pathBuilder.ToString());
        builder.Append(',');

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

    private CurveType inferLegacyTypeFromSegments()
    {
        // Prefer first explicit type, otherwise fallback to Bezier
        var first = ControlPoints.FirstOrDefault(p => p.Type.HasValue);
        if (first.Type.HasValue)
        {
            return first.Type.Value == CurveType.BSpline && first.Degree.HasValue ? CurveType.Bezier : first.Type.Value;
        }
        return CurveType.Bezier;
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
