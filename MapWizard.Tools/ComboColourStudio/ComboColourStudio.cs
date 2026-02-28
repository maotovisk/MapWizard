using System.Drawing;
using BeatmapParser;
using BeatmapParser.Colours;
using BeatmapParser.Enums;
using BeatmapParser.Events;
using BeatmapParser.HitObjects;
using BeatmapParser.Sections;
using MapWizard.Tools.HelperExtensions;
using SkiaSharp;

namespace MapWizard.Tools.ComboColourStudio;

public static class ComboColourStudio
{
    private const int MaxAllowedComboColours = 8;
    private const double MinReadableGeneratedLuminosity = 50d;
    private const double MaxReadableGeneratedLuminosity = 220d;
    private const double LowLuminosityTarget = 75d;
    private const double HighLuminosityTarget = 200d;

    private static readonly Color[] DefaultComboColours =
    [
        Color.FromArgb(255, 255, 192, 0),
        Color.FromArgb(255, 0, 202, 0),
        Color.FromArgb(255, 18, 124, 255),
        Color.FromArgb(255, 242, 24, 57)
    ];

    public static ComboColourProject ImportComboColoursFromBeatmap(string beatmapPath)
    {
        var beatmap = ReadBeatmap(beatmapPath);
        var actingComboColours = GetActingComboColours(beatmap);

        return new ComboColourProject
        {
            ComboColours = CloneComboColours(actingComboColours),
            ColourPoints = [],
            MaxBurstLength = 1
        };
    }

    public static ComboColourProject ExtractColourHaxFromBeatmap(string beatmapPath, int maxBurstLength = 1)
    {
        var beatmap = ReadBeatmap(beatmapPath);
        var project = ExtractColourHaxFromBeatmap(beatmap, maxBurstLength);
        return MinimizeRedundantColourPoints(beatmap, project);
    }

    public static ComboColourProject ExtractColourHaxFromBeatmap(Beatmap beatmap, int maxBurstLength = 1)
    {
        var actingComboColours = GetActingComboColours(beatmap);
        var hitObjects = beatmap.HitObjects.Objects;
        var comboColourCount = actingComboColours.Count;

        if (comboColourCount <= 0)
        {
            throw new InvalidOperationException("No combo colours are available for extraction.");
        }

        var (actualNewCombo, colourIndices) = CalculateComboState(hitObjects, comboColourCount);

        var colourHaxObjects = new List<ColourHaxObject>();
        for (var i = 0; i < hitObjects.Count; i++)
        {
            if (!actualNewCombo[i] || IsSpinner(hitObjects[i]))
            {
                continue;
            }

            colourHaxObjects.Add(new ColourHaxObject(i, hitObjects[i].Time.TotalMilliseconds, colourIndices[i]));
        }

        var project = new ComboColourProject
        {
            ComboColours = CloneComboColours(actingComboColours),
            MaxBurstLength = Math.Max(1, maxBurstLength)
        };

        if (colourHaxObjects.Count == 0)
        {
            return project;
        }

        var breakWindows = GetBreakWindows(beatmap);
        var breakBeforeIndex = BuildBreakBoundaries(colourHaxObjects, breakWindows);
        var sequenceLengthChecks = Enumerable.Range(1, comboColourCount * 2 + 2).ToArray();
        var sequenceStartIndex = 0;
        int[]? lastNormalSequence = null;
        var lastBurst = false;

        while (sequenceStartIndex < colourHaxObjects.Count)
        {
            if (breakBeforeIndex[sequenceStartIndex])
            {
                lastBurst = false;
                lastNormalSequence = null;
            }

            var firstComboObject = colourHaxObjects[sequenceStartIndex];

            var bestSequenceResult = GetBestSequenceAtIndex(
                sequenceStartIndex,
                depth: 3,
                colourHaxObjects,
                hitObjects,
                actualNewCombo,
                sequenceLengthChecks,
                breakBeforeIndex,
                lastBurst,
                lastNormalSequence,
                maxBurstLength: project.MaxBurstLength);

            var bestSequence = bestSequenceResult?.Sequence;

            if (bestSequence is null)
            {
                lastBurst = false;
                sequenceStartIndex += 1;
                continue;
            }

            var bestContribution = GetSequenceContribution(colourHaxObjects, sequenceStartIndex, bestSequence, breakBeforeIndex);

            if (bestContribution <= 0)
            {
                lastBurst = false;
                sequenceStartIndex += 1;
                continue;
            }

            var normalisedSequence = bestSequence
                .Select(index => Mod(index, comboColourCount))
                .ToList();

            var mode = bestContribution == 1 &&
                       GetComboLengthUsingActualNewCombo(hitObjects, actualNewCombo, firstComboObject.HitObjectIndex) <=
                       project.MaxBurstLength
                ? ColourPointMode.Burst
                : ColourPointMode.Normal;

            var canSkipPoint = lastBurst &&
                               !breakBeforeIndex[sequenceStartIndex] &&
                               lastNormalSequence is not null &&
                               IsSubSequence(bestSequence, lastNormalSequence) &&
                               (bestSequence.Length == lastNormalSequence.Length ||
                                bestContribution <= bestSequence.Length);

            if (!canSkipPoint)
            {
                project.ColourPoints.Add(new ComboColourPoint
                {
                    Time = firstComboObject.Time,
                    ColourSequence = normalisedSequence,
                    Mode = mode
                });
            }

            lastBurst = mode == ColourPointMode.Burst;
            sequenceStartIndex += bestContribution;
            lastNormalSequence = mode == ColourPointMode.Burst ? lastNormalSequence : bestSequence;
        }

        return project;
    }

    public static void ApplyProjectToBeatmaps(ComboColourProject project, string[] targetPaths, ComboColourStudioOptions? options = null)
    {
        options ??= new ComboColourStudioOptions();

        foreach (var targetPath in targetPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var beatmap = ReadBeatmap(targetPath);
            ApplyProjectToBeatmap(project, beatmap, options);
            WriteBeatmap(targetPath, beatmap, options.CreateBackupBeforeWrite);
        }
    }

    public static void ApplyProjectToBeatmap(ComboColourProject project, Beatmap beatmap, ComboColourStudioOptions? options = null)
    {
        options ??= new ComboColourStudioOptions();
        ValidateProject(project);

        if (options.UpdateComboColoursSection)
        {
            if (beatmap.Colours is null && options.CreateColoursSectionIfMissing)
            {
                beatmap.Colours = ColoursSection.Decode([]);
            }

            if (beatmap.Colours is not null)
            {
                beatmap.Colours.Combos = CloneComboColours(project.ComboColours);
            }
        }

        if (!options.OverrideHitObjectColourShifts || project.ColourPoints.Count == 0)
        {
            return;
        }

        var hitObjects = beatmap.HitObjects.Objects;
        if (hitObjects.Count == 0)
        {
            return;
        }

        foreach (var hitObject in hitObjects)
        {
            if (IsSpinner(hitObject))
            {
                continue;
            }

            var wasNewCombo = hitObject.NewCombo;
            if (!wasNewCombo)
            {
                hitObject.NewCombo = true;
            }

            hitObject.ComboOffset = 0;

            if (!wasNewCombo)
            {
                hitObject.NewCombo = false;
            }
        }

        var comboCount = project.ComboColours.Count;
        var orderedColourPoints = project.ColourPoints.OrderBy(point => point.Time).ToList();

        var (actualNewCombo, _) = CalculateComboState(hitObjects, comboCount);

        var targetObjects = new List<int>();
        for (var i = 0; i < hitObjects.Count; i++)
        {
            if (actualNewCombo[i] && !IsSpinner(hitObjects[i]))
            {
                targetObjects.Add(i);
            }
        }

        if (targetObjects.Count == 0)
        {
            return;
        }

        var lastColourPointColourIndex = -1;
        var lastColourPoint = orderedColourPoints[0];
        var lastColourIndex = 0;
        var burstExceptions = new HashSet<ComboColourPoint>();

        foreach (var objectIndex in targetObjects)
        {
            var hitObject = hitObjects[objectIndex];
            var comboLength = GetComboLengthUsingRawNewCombo(hitObjects, objectIndex);

            var colourPoint = GetColourPoint(
                orderedColourPoints,
                hitObject.Time.TotalMilliseconds,
                burstExceptions,
                comboLength <= project.MaxBurstLength);

            var colourSequence = colourPoint.ColourSequence;

            if (colourPoint.Mode == ColourPointMode.Burst)
            {
                burstExceptions.Add(colourPoint);
            }

            if (lastColourPointColourIndex != -1 && !ReferenceEquals(lastColourPoint, colourPoint))
            {
                lastColourPointColourIndex = colourSequence.FindIndex(index => index == lastColourIndex);
            }

            var colourPointColourIndex = lastColourPointColourIndex == -1 || colourSequence.Count == 0
                ? 0
                : ReferenceEquals(lastColourPoint, colourPoint)
                    ? Mod(lastColourPointColourIndex + 1, colourSequence.Count)
                    : lastColourPointColourIndex == 0 && colourSequence.Count > 1
                        ? 1
                        : 0;

            var colourIndex = colourSequence.Count == 0
                ? Mod(lastColourIndex + 1, comboCount)
                : colourSequence[colourPointColourIndex];

            var comboIncrease = Mod(colourIndex - lastColourIndex, comboCount);
            var comboSkip = Mod(comboIncrease - 1, comboCount);

            hitObject.ComboOffset = (uint)comboSkip;

            if (!hitObject.NewCombo && comboSkip != 0)
            {
                hitObject.NewCombo = true;
            }

            lastColourPointColourIndex = colourPointColourIndex;
            lastColourPoint = colourPoint;
            lastColourIndex = colourIndex;
        }
    }

    public static string? GetBeatmapBackgroundPath(string beatmapPath)
    {
        var beatmap = ReadBeatmap(beatmapPath);
        var backgroundImage = beatmap.Events.GetBackgroundImage();

        if (string.IsNullOrWhiteSpace(backgroundImage))
        {
            return null;
        }

        var cleanedBackgroundPath = backgroundImage.Trim().Trim('"');
        var beatmapDirectory = Path.GetDirectoryName(beatmapPath);

        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            return null;
        }

        var absolutePath = Path.Combine(beatmapDirectory, cleanedBackgroundPath);
        return File.Exists(absolutePath) ? absolutePath : null;
    }

    public static List<Color> GenerateProminentColours(string imagePath, int maxColours = MaxAllowedComboColours)
    {
        maxColours = Math.Clamp(maxColours, 1, MaxAllowedComboColours);

        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap is null)
        {
            throw new InvalidOperationException("Failed to decode image.");
        }

        var sampleStep = (int)Math.Sqrt((double)(bitmap.Width * bitmap.Height) / 120_000d);
        if (sampleStep < 1)
        {
            sampleStep = 1;
        }

        var buckets = new Dictionary<int, BucketAccumulator>();

        for (var y = 0; y < bitmap.Height; y += sampleStep)
        {
            for (var x = 0; x < bitmap.Width; x += sampleStep)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha < 170)
                {
                    continue;
                }

                var bucketKey = ((pixel.Red >> 4) << 8) | ((pixel.Green >> 4) << 4) | (pixel.Blue >> 4);

                if (buckets.TryGetValue(bucketKey, out var accumulator))
                {
                    accumulator.Add(pixel);
                    buckets[bucketKey] = accumulator;
                }
                else
                {
                    var newAccumulator = new BucketAccumulator();
                    newAccumulator.Add(pixel);
                    buckets[bucketKey] = newAccumulator;
                }
            }
        }

        if (buckets.Count == 0)
        {
            return CloneComboColours(BuildDefaultComboColours())
                .Take(maxColours)
                .Select(combo => combo.Colour)
                .ToList();
        }

        var orderedCandidates = buckets
            .OrderByDescending(pair => pair.Value.Count)
            .Select(pair => MakeColourReadableForGameplay(pair.Value.GetAverageColor()))
            .ToList();

        var selected = new List<Color>();
        const int distanceThresholdSquared = 2_500;

        foreach (var candidate in orderedCandidates)
        {
            if (selected.All(existing => SquaredDistance(existing, candidate) >= distanceThresholdSquared))
            {
                selected.Add(candidate);
                if (selected.Count >= maxColours)
                {
                    break;
                }
            }
        }

        if (selected.Count < maxColours)
        {
            foreach (var candidate in orderedCandidates)
            {
                if (selected.Any(existing => existing.ToArgb() == candidate.ToArgb()))
                {
                    continue;
                }

                selected.Add(candidate);
                if (selected.Count >= maxColours)
                {
                    break;
                }
            }
        }

        if (selected.Count == 0)
        {
            selected.AddRange(BuildDefaultComboColours().Select(combo => combo.Colour));
        }

        return selected.Take(maxColours).ToList();
    }

    private static Beatmap ReadBeatmap(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Beatmap file was not found.", path);
        }

        return Beatmap.Decode(File.ReadAllText(path));
    }

    private static ComboColourProject MinimizeRedundantColourPoints(Beatmap sourceBeatmap, ComboColourProject project)
    {
        if (project.ColourPoints.Count <= 1)
        {
            return project;
        }

        var baselineState = CaptureComboState(sourceBeatmap);
        var sourceBeatmapText = sourceBeatmap.Encode();
        var breakWindows = GetBreakWindows(sourceBeatmap);
        var protectedBoundaryTimes = GetPostBreakComboStartTimes(sourceBeatmap, breakWindows);

        var workingPoints = project.ColourPoints
            .OrderBy(point => point.Time)
            .Select(CloneColourPoint)
            .ToList();

        for (var i = 0; i < workingPoints.Count && workingPoints.Count > 1;)
        {
            if (IsProtectedBoundaryPoint(workingPoints[i].Time, protectedBoundaryTimes))
            {
                i++;
                continue;
            }

            var removed = workingPoints[i];
            workingPoints.RemoveAt(i);

            var candidate = new ComboColourProject
            {
                ComboColours = CloneComboColours(project.ComboColours),
                ColourPoints = workingPoints.Select(CloneColourPoint).ToList(),
                MaxBurstLength = project.MaxBurstLength
            };

            if (!ReproducesComboState(sourceBeatmapText, baselineState, candidate))
            {
                workingPoints.Insert(i, removed);
                i++;
            }
        }

        return new ComboColourProject
        {
            ComboColours = CloneComboColours(project.ComboColours),
            ColourPoints = workingPoints.Select(CloneColourPoint).ToList(),
            MaxBurstLength = project.MaxBurstLength
        };
    }

    private static bool ReproducesComboState(string sourceBeatmapText, IReadOnlyList<(bool NewCombo, uint ComboOffset)> baselineState, ComboColourProject candidate)
    {
        var candidateBeatmap = Beatmap.Decode(sourceBeatmapText);
        ApplyProjectToBeatmap(candidate, candidateBeatmap, new ComboColourStudioOptions
        {
            UpdateComboColoursSection = false,
            OverrideHitObjectColourShifts = true,
            CreateColoursSectionIfMissing = true,
            CreateBackupBeforeWrite = false
        });

        var candidateState = CaptureComboState(candidateBeatmap);
        if (candidateState.Count != baselineState.Count)
        {
            return false;
        }

        for (var i = 0; i < baselineState.Count; i++)
        {
            if (candidateState[i].NewCombo != baselineState[i].NewCombo ||
                candidateState[i].ComboOffset != baselineState[i].ComboOffset)
            {
                return false;
            }
        }

        return true;
    }

    private static List<(bool NewCombo, uint ComboOffset)> CaptureComboState(Beatmap beatmap)
    {
        return beatmap.HitObjects.Objects
            .Select(hitObject => (hitObject.NewCombo, hitObject.ComboOffset))
            .ToList();
    }

    private static ComboColourPoint CloneColourPoint(ComboColourPoint point)
    {
        return new ComboColourPoint
        {
            Time = point.Time,
            Mode = point.Mode,
            ColourSequence = point.ColourSequence.ToList()
        };
    }

    private static List<(double Start, double End)> GetBreakWindows(Beatmap beatmap)
    {
        return beatmap.Events.EventList
            .OfType<Break>()
            .Select(breakEvent => (breakEvent.StartTime.TotalMilliseconds, breakEvent.EndTime.TotalMilliseconds))
            .Where(window => window.Item2 > window.Item1)
            .OrderBy(window => window.Item1)
            .Select(window => (Start: window.Item1, End: window.Item2))
            .ToList();
    }

    private static bool[] BuildBreakBoundaries(IReadOnlyList<ColourHaxObject> colourHaxObjects, IReadOnlyList<(double Start, double End)> breakWindows)
    {
        var breakBeforeIndex = new bool[colourHaxObjects.Count];
        if (colourHaxObjects.Count <= 1 || breakWindows.Count == 0)
        {
            return breakBeforeIndex;
        }

        for (var i = 1; i < colourHaxObjects.Count; i++)
        {
            if (HasBreakBetween(colourHaxObjects[i - 1].Time, colourHaxObjects[i].Time, breakWindows))
            {
                breakBeforeIndex[i] = true;
            }
        }

        return breakBeforeIndex;
    }

    private static bool HasBreakBetween(double fromTime, double toTime, IReadOnlyList<(double Start, double End)> breakWindows)
    {
        if (toTime <= fromTime)
        {
            return false;
        }

        foreach (var breakWindow in breakWindows)
        {
            if (breakWindow.Start >= toTime)
            {
                break;
            }

            if (breakWindow.End > fromTime && breakWindow.Start < toTime)
            {
                return true;
            }
        }

        return false;
    }

    private static List<double> GetPostBreakComboStartTimes(Beatmap beatmap, IReadOnlyList<(double Start, double End)> breakWindows)
    {
        if (breakWindows.Count == 0)
        {
            return [];
        }

        var comboCount = GetActingComboColours(beatmap).Count;
        if (comboCount <= 0)
        {
            return [];
        }

        var hitObjects = beatmap.HitObjects.Objects;
        var (actualNewCombo, _) = CalculateComboState(hitObjects, comboCount);
        var comboStartTimes = new List<double>();

        for (var i = 0; i < hitObjects.Count; i++)
        {
            if (actualNewCombo[i] && !IsSpinner(hitObjects[i]))
            {
                comboStartTimes.Add(hitObjects[i].Time.TotalMilliseconds);
            }
        }

        if (comboStartTimes.Count == 0)
        {
            return [];
        }

        var protectedTimes = new List<double>();
        foreach (var breakWindow in breakWindows)
        {
            double? nextComboStart = null;

            foreach (var comboStartTime in comboStartTimes)
            {
                if (comboStartTime >= breakWindow.End)
                {
                    nextComboStart = comboStartTime;
                    break;
                }
            }

            if (nextComboStart is null || IsProtectedBoundaryPoint(nextComboStart.Value, protectedTimes))
            {
                continue;
            }

            protectedTimes.Add(nextComboStart.Value);
        }

        return protectedTimes;
    }

    private static bool IsProtectedBoundaryPoint(double time, IReadOnlyList<double> protectedTimes)
    {
        return protectedTimes.Any(protectedTime => Math.Abs(protectedTime - time) < 0.5d);
    }

    private static void WriteBeatmap(string path, Beatmap beatmap, bool createBackup)
    {
        if (createBackup)
        {
            TryCreateBackup(path);
        }

        File.WriteAllText(path, beatmap.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));
    }

    private static void TryCreateBackup(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        BeatmapBackupHelper.CreateBackupCopy(path);
    }

    private static List<ComboColour> GetActingComboColours(Beatmap beatmap)
    {
        if (beatmap.Colours?.Combos is { Count: > 0 })
        {
            return CloneComboColours(beatmap.Colours.Combos);
        }

        return BuildDefaultComboColours();
    }

    private static List<ComboColour> BuildDefaultComboColours()
    {
        return DefaultComboColours
            .Select((colour, index) => new ComboColour((uint)(index + 1), colour))
            .ToList();
    }

    private static List<ComboColour> CloneComboColours(IEnumerable<ComboColour> comboColours)
    {
        return comboColours
            .Take(MaxAllowedComboColours)
            .Select((colour, index) => new ComboColour((uint)(index + 1), Color.FromArgb(255, colour.Colour.R, colour.Colour.G, colour.Colour.B)))
            .ToList();
    }

    private static (bool[] ActualNewCombo, int[] ColourIndices) CalculateComboState(IReadOnlyList<IHitObject> hitObjects, int comboColourCount)
    {
        var actualNewCombo = new bool[hitObjects.Count];
        var colourIndices = new int[hitObjects.Count];

        IHitObject? previous = null;
        var colourIndex = 0;

        for (var i = 0; i < hitObjects.Count; i++)
        {
            var hitObject = hitObjects[i];
            var isActualNewCombo = IsActualNewCombo(hitObject, previous);
            actualNewCombo[i] = isActualNewCombo;

            if (isActualNewCombo)
            {
                var colourIncrement = IsSpinner(hitObject)
                    ? (int)hitObject.ComboOffset
                    : (int)hitObject.ComboOffset + 1;

                colourIndex = Mod(colourIndex + colourIncrement, comboColourCount);
            }

            colourIndices[i] = colourIndex;
            previous = hitObject;
        }

        return (actualNewCombo, colourIndices);
    }

    private static bool IsActualNewCombo(IHitObject hitObject, IHitObject? previousHitObject)
    {
        return hitObject.NewCombo || IsSpinner(hitObject) || previousHitObject is null || IsSpinner(previousHitObject);
    }

    private static bool IsSpinner(IHitObject hitObject)
    {
        return hitObject.Type == HitObjectType.Spinner;
    }

    private static int GetComboLengthUsingActualNewCombo(IReadOnlyList<IHitObject> hitObjects, bool[] actualNewCombo, int firstHitObjectIndex)
    {
        var index = firstHitObjectIndex;
        var count = 1;

        while (++index < hitObjects.Count && !actualNewCombo[index])
        {
            count++;
        }

        return count;
    }

    private static int GetComboLengthUsingRawNewCombo(IReadOnlyList<IHitObject> hitObjects, int firstHitObjectIndex)
    {
        var index = firstHitObjectIndex;
        var count = 1;

        while (++index < hitObjects.Count)
        {
            if (hitObjects[index].NewCombo)
            {
                return count;
            }

            count++;
        }

        return count;
    }

    private static SequenceSearchResult? GetBestSequenceAtIndex(
        int sequenceStartIndex,
        int depth,
        IReadOnlyList<ColourHaxObject> colourHaxObjects,
        IReadOnlyList<IHitObject> hitObjects,
        bool[] actualNewCombo,
        int[] sequenceLengthChecks,
        IReadOnlyList<bool> breakBeforeIndex,
        bool lastBurst,
        int[]? lastNormalSequence,
        int maxBurstLength)
    {
        if (sequenceStartIndex >= colourHaxObjects.Count)
        {
            return null;
        }

        var firstComboHitObject = colourHaxObjects[sequenceStartIndex];

        var sequences = sequenceLengthChecks
            .Select(length => GetColourSequence(colourHaxObjects, sequenceStartIndex, length, breakBeforeIndex))
            .ToArray();

        var contributions = sequences
            .Select(sequence => GetSequenceContribution(colourHaxObjects, sequenceStartIndex, sequence, breakBeforeIndex))
            .ToArray();

        double bestScore = double.NegativeInfinity;
        int[]? bestSequence = null;
        var bestContribution = 0;
        var bestCost = double.PositiveInfinity;

        for (var i = 0; i < sequences.Length; i++)
        {
            var sequence = sequences[i];
            if (sequence is null)
            {
                continue;
            }

            var contribution = contributions[i];
            if (contribution <= 0)
            {
                continue;
            }

            var burst = contribution == 1 &&
                        GetComboLengthUsingActualNewCombo(hitObjects, actualNewCombo, firstComboHitObject.HitObjectIndex) <=
                        maxBurstLength;

            var cost = (double)sequence.Length;

            if (lastBurst &&
                lastNormalSequence is not null &&
                IsSubSequence(sequence, lastNormalSequence) &&
                (sequence.Length == lastNormalSequence.Length || contribution <= sequence.Length))
            {
                cost = 0;
            }

            if (depth > 0)
            {
                var nextBest = GetBestSequenceAtIndex(
                    sequenceStartIndex + contribution,
                    depth - 1,
                    colourHaxObjects,
                    hitObjects,
                    actualNewCombo,
                    sequenceLengthChecks,
                    breakBeforeIndex,
                    burst,
                    burst ? lastNormalSequence : sequence,
                    maxBurstLength);

                if (nextBest is not null)
                {
                    contribution += nextBest.Value.Contribution / 2;
                    cost += nextBest.Value.Cost / 2;
                }
            }

            var score = contribution / cost;

            if (bestSequence is not null &&
                (score < bestScore || (Math.Abs(score - bestScore) < double.Epsilon && cost >= bestCost)))
            {
                continue;
            }

            bestScore = score;
            bestSequence = sequence;
            bestContribution = contribution;
            bestCost = cost;

            if (double.IsPositiveInfinity(bestScore))
            {
                break;
            }
        }

        return bestSequence is null
            ? null
            : new SequenceSearchResult(bestSequence, bestContribution, bestCost);
    }

    private static bool IsSubSequence(int[] sequence, int[]? biggerSequence)
    {
        if (biggerSequence is null || sequence.Length > biggerSequence.Length)
        {
            return false;
        }

        return !sequence.Where((t, i) => t != biggerSequence[i]).Any();
    }

    private static int[]? GetColourSequence(
        IReadOnlyList<ColourHaxObject> hitObjects,
        int startIndex,
        int sequenceLength,
        IReadOnlyList<bool> breakBeforeIndex)
    {
        var colourSequence = new int[sequenceLength];

        for (var i = 0; i < sequenceLength; i++)
        {
            if (startIndex + i >= hitObjects.Count)
            {
                return null;
            }

            if (i > 0 && breakBeforeIndex[startIndex + i])
            {
                return null;
            }

            colourSequence[i] = hitObjects[startIndex + i].ColourIndex;
        }

        return colourSequence;
    }

    private static int GetSequenceContribution(
        IReadOnlyList<ColourHaxObject> hitObjects,
        int startIndex,
        IReadOnlyList<int>? colourSequence,
        IReadOnlyList<bool> breakBeforeIndex)
    {
        if (colourSequence is null || colourSequence.Count == 0)
        {
            return 0;
        }

        var index = startIndex;
        var sequenceIndex = 0;
        var score = 0;

        while (index < hitObjects.Count && hitObjects[index].ColourIndex == colourSequence[sequenceIndex])
        {
            score++;
            index++;

            if (index >= hitObjects.Count || breakBeforeIndex[index])
            {
                break;
            }

            sequenceIndex = Mod(sequenceIndex + 1, colourSequence.Count);
        }

        return score;
    }

    private static ComboColourPoint GetColourPoint(
        IReadOnlyList<ComboColourPoint> colourPoints,
        double time,
        IReadOnlySet<ComboColourPoint> exceptions,
        bool includeBurst)
    {
        ComboColourPoint? closestBefore = null;

        foreach (var colourPoint in colourPoints)
        {
            if (exceptions.Contains(colourPoint))
            {
                continue;
            }

            if (colourPoint.Time <= time + 5 &&
                (colourPoint.Mode != ColourPointMode.Burst || (colourPoint.Time >= time - 5 && includeBurst)))
            {
                closestBefore = colourPoint;
            }
        }

        if (closestBefore is not null)
        {
            return closestBefore;
        }

        foreach (var colourPoint in colourPoints)
        {
            if (!exceptions.Contains(colourPoint) && colourPoint.Mode != ColourPointMode.Burst)
            {
                return colourPoint;
            }
        }

        return colourPoints[0];
    }

    private static void ValidateProject(ComboColourProject project)
    {
        if (project.ComboColours.Count == 0)
        {
            throw new ArgumentException("Please add at least one combo colour before exporting.");
        }

        if (project.ComboColours.Count > MaxAllowedComboColours)
        {
            throw new ArgumentException($"osu! supports up to {MaxAllowedComboColours} combo colours.");
        }

        foreach (var colourPoint in project.ColourPoints)
        {
            foreach (var colourIndex in colourPoint.ColourSequence)
            {
                if (colourIndex < 0 || colourIndex >= project.ComboColours.Count)
                {
                    throw new ArgumentException(
                        $"Colour point at {colourPoint.Time:0.##}ms references combo index {colourIndex + 1}, but only {project.ComboColours.Count} combo colours exist.");
                }
            }
        }
    }

    private static int Mod(int value, int modulo)
    {
        if (modulo <= 0)
        {
            return 0;
        }

        var result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static int SquaredDistance(Color a, Color b)
    {
        var dr = a.R - b.R;
        var dg = a.G - b.G;
        var db = a.B - b.B;

        return dr * dr + dg * dg + db * db;
    }

    public static Color MakeColourReadableForGameplay(Color colour)
    {
        var luminosity = GetPerceivedLuminosity(colour);

        if (luminosity <= MinReadableGeneratedLuminosity)
        {
            var liftScale = LowLuminosityTarget / Math.Max(1d, luminosity);
            return ScaleColour(colour, liftScale);
        }

        if (luminosity >= MaxReadableGeneratedLuminosity)
        {
            var lowerScale = HighLuminosityTarget / Math.Max(1d, luminosity);
            return ScaleColour(colour, lowerScale);
        }

        return colour;
    }

    private static Color ScaleColour(Color colour, double scale)
    {
        return Color.FromArgb(
            255,
            (int)Math.Clamp(Math.Round(colour.R * scale), 0, 255),
            (int)Math.Clamp(Math.Round(colour.G * scale), 0, 255),
            (int)Math.Clamp(Math.Round(colour.B * scale), 0, 255));
    }

    private static double GetPerceivedLuminosity(Color colour)
    {
        // WCAG-style luma in 0-255 space; keeps hue while lifting only too-dark colours.
        return 0.2126d * colour.R + 0.7152d * colour.G + 0.0722d * colour.B;
    }

    private readonly struct ColourHaxObject(int hitObjectIndex, double time, int colourIndex)
    {
        public int HitObjectIndex { get; } = hitObjectIndex;
        public double Time { get; } = time;
        public int ColourIndex { get; } = colourIndex;
    }

    private readonly struct SequenceSearchResult(int[] sequence, int contribution, double cost)
    {
        public int[] Sequence { get; } = sequence;
        public int Contribution { get; } = contribution;
        public double Cost { get; } = cost;
    }

    private struct BucketAccumulator
    {
        public int Count { get; private set; }
        private long _sumR;
        private long _sumG;
        private long _sumB;

        public void Add(SKColor colour)
        {
            Count++;
            _sumR += colour.Red;
            _sumG += colour.Green;
            _sumB += colour.Blue;
        }

        public Color GetAverageColor()
        {
            if (Count <= 0)
            {
                return Color.White;
            }

            return Color.FromArgb(
                255,
                (int)Math.Clamp(_sumR / Count, 0, 255),
                (int)Math.Clamp(_sumG / Count, 0, 255),
                (int)Math.Clamp(_sumB / Count, 0, 255));
        }
    }
}
