using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using BeatmapParser;
using BeatmapParser.Enums;
using BeatmapParser.HitObjects;
using BeatmapParser.HitObjects.HitSounds;
using BeatmapParser.TimingPoints;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using MapWizard.Tools.HelperExtensions;
using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Tools.HitSounds.Event;
using MapWizard.Tools.HitSounds.Extensions;
using MapWizard.Tools.HitSounds.Timeline;
using MapWizard.Tools.MapCleaner.Snapping;

namespace MapWizard.Desktop.Services.HitsoundService;

public class HitSoundService : IHitSoundService
{
    private const double RedlineTimeToleranceMs = 0.5d;
    private const double BeatLengthToleranceMs = 0.001d;
    private const int VisualizerExportLeniencyMs = 5;
    // Guards against invalid/near-zero intervals when generating snap ticks.
    private const double SnapTickStepToleranceMs = 0.00001d;
    // Inclusive segment-end tolerance to absorb floating-point drift at boundaries.
    private const double SnapTickSegmentEndToleranceMs = 0.0001d;
    private static readonly Vector2 HitsoundDiffCirclePosition = new(256, 192);
    private static readonly IReadOnlyList<SnapDivisor> VisualizerDivisors =
        Enumerable.Range(1, 16).Select(x => new SnapDivisor(1, x)).ToList();

    public HitSoundTimingCompatibilityReport AnalyzeTimingCompatibility(string sourcePath, string[] targetPaths)
    {
        var sourceFile = Beatmap.Decode(File.ReadAllText(sourcePath));
        var results = new List<HitSoundTimingCompatibilityTargetResult>(targetPaths.Length);

        foreach (var targetPath in targetPaths)
        {
            var targetFile = Beatmap.Decode(File.ReadAllText(targetPath));
            results.Add(CompareTiming(sourceFile, targetFile, targetPath));
        }

        return new HitSoundTimingCompatibilityReport
        {
            Targets = results
        };
    }

    public bool CopyHitsounds(string sourcePath, string[] targetPaths, HitSoundCopierOptions options)
    {
        try
        {
            HitSoundCopier.CopyFromBeatmapToTarget(sourcePath: sourcePath, targetPath: targetPaths, options: options);
        }
        catch (Exception ex)
        {
            MapWizardLogger.LogException(ex);
            Console.WriteLine(ex.Message);
            return false;
        }
        
        return true;
    }

    public HitSoundVisualizerDocument LoadHitsoundVisualizerDocument(string beatmapPath)
    {
        var beatmap = Beatmap.Decode(File.ReadAllText(beatmapPath));
        var timeline = beatmap.BuildTimeline();
        var points = BuildVisualizerPoints(timeline);
        var sampleChanges = BuildVisualizerSampleChanges(timeline);

        var endTimeMs = ResolveEndTimeMs(beatmap, points, sampleChanges);
        var snapTicks = BuildSnapTicks(beatmap, endTimeMs);

        var artist = string.IsNullOrWhiteSpace(beatmap.MetadataSection.ArtistUnicode)
            ? beatmap.MetadataSection.Artist
            : beatmap.MetadataSection.ArtistUnicode;
        var title = string.IsNullOrWhiteSpace(beatmap.MetadataSection.TitleUnicode)
            ? beatmap.MetadataSection.Title
            : beatmap.MetadataSection.TitleUnicode;
        var mapsetDirectory = Path.GetDirectoryName(beatmapPath) ?? string.Empty;
        var audioFileName = beatmap.GeneralSection.AudioFilename;
        var audioFilePath = string.IsNullOrWhiteSpace(audioFileName) || string.IsNullOrWhiteSpace(mapsetDirectory)
            ? string.Empty
            : Path.Combine(mapsetDirectory, audioFileName);

        return new HitSoundVisualizerDocument
        {
            BeatmapPath = beatmapPath,
            MapsetDirectoryPath = mapsetDirectory,
            AudioFilePath = File.Exists(audioFilePath) ? audioFilePath : string.Empty,
            DisplayTitle = $"{artist} - {title} [{beatmap.MetadataSection.Version}]",
            EndTimeMs = endTimeMs,
            Timeline = timeline,
            Points = points,
            SampleChanges = sampleChanges,
            SnapTicks = snapTicks
        };
    }

    public IReadOnlyList<HitSoundVisualizerSnapTick> BuildHitsoundVisualizerSnapTicks(string beatmapPath, double endTimeMs)
    {
        var beatmap = Beatmap.Decode(File.ReadAllText(beatmapPath));
        return BuildSnapTicks(beatmap, Math.Max(1000, endTimeMs));
    }

    public bool ApplyVisualizerTimelineToBeatmap(string targetBeatmapPath, HitSoundTimeline timeline, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(targetBeatmapPath) || !File.Exists(targetBeatmapPath))
            {
                errorMessage = "Target beatmap file was not found.";
                return false;
            }

            var normalizedTimeline = NormalizeTimelineForBeatmapEncoding(timeline);
            var targetBeatmap = Beatmap.Decode(File.ReadAllText(targetBeatmapPath));
            var options = BuildVisualizerExportOptions();

            targetBeatmap.ApplyNonDraggableHitSounds(normalizedTimeline.NonDraggableSoundTimeline, options);
            targetBeatmap.ApplySampleTimeline(normalizedTimeline.SampleSetTimeline, options);
            targetBeatmap.TimingPoints?.RemoveRedundantGreenLines();

            BeatmapBackupHelper.CreateBackupCopy(targetBeatmapPath);
            WriteBeatmap(targetBeatmapPath, targetBeatmap);
            return true;
        }
        catch (Exception ex)
        {
            MapWizardLogger.LogException(ex);
            errorMessage = ex.Message;
            return false;
        }
    }

    public bool ExportVisualizerHitsoundDiff(
        string sourceBeatmapPath,
        HitSoundTimeline timeline,
        out string exportedBeatmapPath,
        out string errorMessage)
    {
        exportedBeatmapPath = string.Empty;
        errorMessage = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(sourceBeatmapPath) || !File.Exists(sourceBeatmapPath))
            {
                errorMessage = "Source beatmap file was not found.";
                return false;
            }

            var sourceBeatmap = Beatmap.Decode(File.ReadAllText(sourceBeatmapPath));
            var normalizedTimeline = NormalizeTimelineForBeatmapEncoding(timeline);
            if (normalizedTimeline.NonDraggableSoundTimeline.SoundEvents.Count == 0)
            {
                errorMessage = "There are no hitsound points to export.";
                return false;
            }

            // Clone through encode/decode so we can keep all map metadata while replacing hitobjects.
            var exportedBeatmap = Beatmap.Decode(sourceBeatmap.Encode());
            exportedBeatmap.GeneralSection.Mode = Ruleset.Osu;
            exportedBeatmap.GeneralSection.StackLeniency = 0.0d;

            var versionName = BuildHitsoundDiffVersionName(sourceBeatmap.MetadataSection.Version);
            exportedBeatmap.MetadataSection.Version = versionName;
            exportedBeatmap.HitObjects.Objects = BuildCircleOnlyHitObjects(normalizedTimeline.NonDraggableSoundTimeline);

            var options = BuildVisualizerExportOptions();
            exportedBeatmap.ApplySampleTimeline(normalizedTimeline.SampleSetTimeline, options);
            exportedBeatmap.TimingPoints?.RemoveRedundantGreenLines();

            exportedBeatmapPath = BuildUniqueHitsoundDiffPath(sourceBeatmapPath, versionName);
            WriteBeatmap(exportedBeatmapPath, exportedBeatmap);
            return true;
        }
        catch (Exception ex)
        {
            MapWizardLogger.LogException(ex);
            errorMessage = ex.Message;
            return false;
        }
    }

    private static HitSoundTimingCompatibilityTargetResult CompareTiming(Beatmap source, Beatmap target, string targetPath)
    {
        var sourceRedlines = GetRedlines(source);
        var targetRedlines = GetRedlines(target);

        if (sourceRedlines.Count == 0 || targetRedlines.Count == 0)
        {
            return new HitSoundTimingCompatibilityTargetResult
            {
                TargetPath = targetPath,
                Kind = HitSoundTimingCompatibilityKind.Compatible,
                Details = "One of the beatmaps has no redlines to compare."
            };
        }

        if (sourceRedlines.Count != targetRedlines.Count)
        {
            return new HitSoundTimingCompatibilityTargetResult
            {
                TargetPath = targetPath,
                Kind = HitSoundTimingCompatibilityKind.TimingMismatch,
                Details = $"Different redline count ({sourceRedlines.Count} vs {targetRedlines.Count})."
            };
        }

        double? offsetMs = null;

        for (var i = 0; i < sourceRedlines.Count; i++)
        {
            var sourceRedline = sourceRedlines[i];
            var targetRedline = targetRedlines[i];

            if (Math.Abs(sourceRedline.BeatLength - targetRedline.BeatLength) > BeatLengthToleranceMs)
            {
                return new HitSoundTimingCompatibilityTargetResult
                {
                    TargetPath = targetPath,
                    Kind = HitSoundTimingCompatibilityKind.TimingMismatch,
                    Details = $"Different BPM/redline beat length at redline #{i + 1}."
                };
            }

            var currentOffsetMs = targetRedline.Time.TotalMilliseconds - sourceRedline.Time.TotalMilliseconds;
            if (offsetMs is null)
            {
                offsetMs = currentOffsetMs;
                continue;
            }

            if (Math.Abs(currentOffsetMs - offsetMs.Value) > RedlineTimeToleranceMs)
            {
                return new HitSoundTimingCompatibilityTargetResult
                {
                    TargetPath = targetPath,
                    Kind = HitSoundTimingCompatibilityKind.TimingMismatch,
                    Details = "Redline positions are not aligned by a constant offset."
                };
            }
        }

        var resolvedOffsetMs = offsetMs ?? 0d;
        if (Math.Abs(resolvedOffsetMs) <= RedlineTimeToleranceMs)
        {
            return new HitSoundTimingCompatibilityTargetResult
            {
                TargetPath = targetPath,
                Kind = HitSoundTimingCompatibilityKind.Compatible
            };
        }

        return new HitSoundTimingCompatibilityTargetResult
        {
            TargetPath = targetPath,
            Kind = HitSoundTimingCompatibilityKind.OffsetOnlyMismatch,
            OffsetMs = resolvedOffsetMs,
            SuggestedLeniencyMs = Math.Max(1, (int)Math.Ceiling(Math.Abs(resolvedOffsetMs))),
            Details = $"Constant offset detected ({resolvedOffsetMs:+0.###;-0.###;0} ms)."
        };
    }

    private static IReadOnlyList<UninheritedTimingPoint> GetRedlines(Beatmap beatmap)
    {
        return beatmap.TimingPoints?.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList()
            ?? [];
    }

    private static List<HitSoundVisualizerPoint> BuildVisualizerPoints(HitSoundTimeline timeline)
    {
        var result = new List<HitSoundVisualizerPoint>();
        var nextId = 1;

        AddPointsFromTimeline(timeline.NonDraggableSoundTimeline, false, timeline.SampleSetTimeline, result, ref nextId);

        return result
            .OrderBy(x => x.TimeMs)
            .ThenBy(x => HitSoundSortOrder(x.HitSound))
            .ThenBy(x => SampleSetSortOrder(x.SampleSet))
            .ToList();
    }

    private static List<HitSoundVisualizerSampleChange> BuildVisualizerSampleChanges(HitSoundTimeline timeline)
    {
        var sorted = timeline.SampleSetTimeline.HitSamples
            .OrderBy(x => x.Time)
            .Select(x => new HitSoundVisualizerSampleChange
            {
                TimeMs = StableSnapEngine.StableRound(x.Time),
                SampleSet = NormalizeSampleSet(x.Sample, SampleSet.Normal),
                Index = NormalizeSampleIndex(x.Index),
                Volume = NormalizeSampleVolume(x.Volume)
            })
            .ToList();

        if (sorted.Count <= 1)
        {
            return sorted;
        }

        var normalized = new List<HitSoundVisualizerSampleChange>(sorted.Count);
        foreach (var change in sorted)
        {
            if (normalized.Count == 0)
            {
                normalized.Add(change);
                continue;
            }

            var last = normalized[^1];

            // Multiple timing points can collapse to the same rounded millisecond; keep the latest effective value.
            if (last.TimeMs == change.TimeMs)
            {
                normalized[^1] = change;
                continue;
            }

            // Skip redundant entries that don't change the effective sample timeline state.
            if (HasSameSampleState(last, change))
            {
                continue;
            }

            normalized.Add(change);
        }

        return normalized;
    }

    private static bool HasSameSampleState(HitSoundVisualizerSampleChange a, HitSoundVisualizerSampleChange b)
    {
        return a.SampleSet == b.SampleSet &&
               a.Index == b.Index &&
               a.Volume == b.Volume;
    }

    private static void AddPointsFromTimeline(
        SoundTimeline soundTimeline,
        bool isDraggable,
        SampleSetTimeline sampleSetTimeline,
        ICollection<HitSoundVisualizerPoint> target,
        ref int nextId)
    {
        foreach (var soundEvent in soundTimeline.SoundEvents)
        {
            var sourceTimeMs = soundEvent.Time.TotalMilliseconds;
            var timeMs = StableSnapEngine.StableRound(soundEvent.Time.TotalMilliseconds);
            var effectiveSampleChange = sampleSetTimeline.GetCurrentSampleAtTime(sourceTimeMs, leniency: 0);
            var timelineSample = NormalizeSampleSet(effectiveSampleChange?.Sample ?? SampleSet.Normal, SampleSet.Normal);
            var resolvedNormalSample = NormalizeSampleSet(soundEvent.NormalSample, timelineSample);
            var normalUsesAutoSampleSet = !IsExplicitSampleSet(soundEvent.NormalSample);

            // Normalize parser flags into atomic sounds.
            // This avoids treating combined flags as unknown "normal" lanes, which can duplicate hitnormal playback.
            var hitSounds = ExpandAtomicHitSounds(soundEvent.HitSounds);

            foreach (var hitSound in hitSounds)
            {
                var rawSample = hitSound == HitSound.Normal
                    ? soundEvent.NormalSample
                    : soundEvent.AdditionSample;
                var usesAutoSampleSet = hitSound == HitSound.Normal
                    ? normalUsesAutoSampleSet
                    : !IsExplicitSampleSet(soundEvent.AdditionSample);
                var resolvedSample = hitSound == HitSound.Normal
                    ? resolvedNormalSample
                    : NormalizeSampleSet(rawSample, resolvedNormalSample);

                target.Add(new HitSoundVisualizerPoint
                {
                    Id = nextId++,
                    TimeMs = timeMs,
                    HitSound = hitSound,
                    SampleSet = resolvedSample,
                    SampleIndexOverride = soundEvent.SampleIndexOverride,
                    SampleVolumeOverridePercent = soundEvent.SampleVolumeOverride,
                    IsAutoSampleSet = usesAutoSampleSet,
                    IsDraggable = isDraggable
                });
            }
        }
    }

    private static double ResolveEndTimeMs(
        Beatmap beatmap,
        IReadOnlyCollection<HitSoundVisualizerPoint> points,
        IReadOnlyCollection<HitSoundVisualizerSampleChange> sampleChanges)
    {
        var maxPoint = points.Count == 0 ? 0 : points.Max(x => x.TimeMs);
        var maxSample = sampleChanges.Count == 0 ? 0 : sampleChanges.Max(x => x.TimeMs);
        var maxTiming = beatmap.TimingPoints?.TimingPointList.Count > 0
            ? beatmap.TimingPoints.TimingPointList.Max(x => x.Time.TotalMilliseconds)
            : 0;

        var resolved = Math.Max(maxPoint, Math.Max(maxSample, maxTiming));
        return Math.Max(1000, resolved + 500);
    }

    private static IReadOnlyList<HitSoundVisualizerSnapTick> BuildSnapTicks(Beatmap beatmap, double endTimeMs)
    {
        if (beatmap.TimingPoints == null)
        {
            return [];
        }

        var redlines = beatmap.TimingPoints.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();

        if (redlines.Count == 0)
        {
            return [];
        }

        var ticksByTimeAndDenominator = new Dictionary<(int TimeMs, int Denominator), HitSoundVisualizerSnapTick>();

        for (var redlineIndex = 0; redlineIndex < redlines.Count; redlineIndex++)
        {
            var redline = redlines[redlineIndex];
            // Keep the original redline phase (including negative offsets) so snap lines align correctly.
            var startMs = redline.Time.TotalMilliseconds;
            var nextRedlineMs = redlineIndex < redlines.Count - 1
                ? redlines[redlineIndex + 1].Time.TotalMilliseconds
                : endTimeMs;
            var segmentEndMs = Math.Min(endTimeMs, nextRedlineMs);
            var beatLength = Math.Abs(redline.BeatLength);
            var beatsPerMeasure = redline.TimeSignature <= 0 ? 4 : redline.TimeSignature;

            if (beatLength <= SnapTickStepToleranceMs || segmentEndMs < startMs)
            {
                continue;
            }

            foreach (var divisor in VisualizerDivisors)
            {
                var step = beatLength * divisor.Numerator / divisor.Denominator;
                if (step <= SnapTickStepToleranceMs)
                {
                    continue;
                }

                var maxStepIndex = (int)Math.Floor((segmentEndMs - startMs + SnapTickSegmentEndToleranceMs) / step);
                for (var stepIndex = 0; stepIndex <= maxStepIndex; stepIndex++)
                {
                    var tickTime = startMs + (stepIndex * step);
                    if (tickTime > segmentEndMs + SnapTickSegmentEndToleranceMs)
                    {
                        // Safety guard against floating-point accumulation on boundary indices.
                        break;
                    }

                    var rounded = StableSnapEngine.StableRound(tickTime);
                    if (rounded < 0 || rounded > endTimeMs + 1)
                    {
                        continue;
                    }

                    var newTick = new HitSoundVisualizerSnapTick
                    {
                        TimeMs = rounded,
                        ExactTimeMs = tickTime,
                        Label = divisor.ToString(),
                        Strength = TickStrength(divisor),
                        Denominator = divisor.Denominator,
                        IsMeasureLine = divisor.Denominator == 1 && stepIndex % beatsPerMeasure == 0
                    };

                    var key = (rounded, divisor.Denominator);
                    if (!ticksByTimeAndDenominator.TryGetValue(key, out var current))
                    {
                        ticksByTimeAndDenominator[key] = newTick;
                        continue;
                    }

                    current.IsMeasureLine |= newTick.IsMeasureLine;
                }
            }
        }

        return ticksByTimeAndDenominator.Values
            .OrderBy(x => x.TimeMs)
            .ThenBy(x => x.Denominator)
            .ToList();
    }

    private static int TickStrength(SnapDivisor divisor)
    {
        return divisor.Denominator switch
        {
            1 => 4,
            <= 4 => 3,
            <= 8 => 2,
            _ => 1
        };
    }

    private static IReadOnlyList<HitSound> ExpandAtomicHitSounds(IEnumerable<HitSound>? sourceHitSounds)
    {
        var combinedFlags = 0;
        if (sourceHitSounds is not null)
        {
            foreach (var hitSound in sourceHitSounds)
            {
                combinedFlags |= (int)hitSound;
            }
        }

        // Hitnormal is implicit in osu! hittables; represent it explicitly in the visualizer.
        var resolved = new List<HitSound> { HitSound.Normal };

        if (ContainsHitSoundFlag(combinedFlags, HitSound.Whistle))
        {
            resolved.Add(HitSound.Whistle);
        }

        if (ContainsHitSoundFlag(combinedFlags, HitSound.Finish))
        {
            resolved.Add(HitSound.Finish);
        }

        if (ContainsHitSoundFlag(combinedFlags, HitSound.Clap))
        {
            resolved.Add(HitSound.Clap);
        }

        return resolved;
    }

    private static bool ContainsHitSoundFlag(int combinedFlags, HitSound flag)
    {
        var numericFlag = (int)flag;
        if (numericFlag == 0)
        {
            return combinedFlags == 0;
        }

        return (combinedFlags & numericFlag) == numericFlag;
    }

    private static int HitSoundSortOrder(HitSound hitSound)
    {
        return hitSound switch
        {
            HitSound.Normal => 0,
            HitSound.Whistle => 1,
            HitSound.Finish => 2,
            HitSound.Clap => 3,
            _ => 9
        };
    }

    private static int SampleSetSortOrder(SampleSet sampleSet)
    {
        return sampleSet switch
        {
            SampleSet.Normal => 0,
            SampleSet.Soft => 1,
            SampleSet.Drum => 2,
            _ => 9
        };
    }

    private static SampleSet NormalizeSampleSet(SampleSet rawSampleSet, SampleSet fallback)
    {
        if (IsExplicitSampleSet(rawSampleSet))
        {
            return rawSampleSet;
        }

        return IsExplicitSampleSet(fallback)
            ? fallback
            : SampleSet.Normal;
    }

    private static bool IsExplicitSampleSet(SampleSet sampleSet)
    {
        return sampleSet is SampleSet.Normal or SampleSet.Soft or SampleSet.Drum;
    }

    private static int NormalizeSampleIndex(int rawIndex)
    {
        return rawIndex > 0 ? rawIndex : 1;
    }

    private static int NormalizeSampleVolume(double rawVolume)
    {
        var rounded = (int)Math.Round(rawVolume);
        return rounded <= 0 ? 100 : Math.Clamp(rounded, 1, 100);
    }

    private static HitSoundTimeline NormalizeTimelineForBeatmapEncoding(HitSoundTimeline source)
    {
        var sourceTimeline = source ?? new HitSoundTimeline();

        var nonDraggable = sourceTimeline.NonDraggableSoundTimeline?.SoundEvents
                               ?.OrderBy(x => x.Time.TotalMilliseconds)
                               .Select(NormalizeSoundEventForBeatmapEncoding)
                               .ToList()
                           ?? [];
        var draggable = sourceTimeline.DraggableSoundTimeline?.SoundEvents
                            ?.OrderBy(x => x.Time.TotalMilliseconds)
                            .Select(NormalizeSoundEventForBeatmapEncoding)
                            .ToList()
                        ?? [];
        var sampleChanges = sourceTimeline.SampleSetTimeline?.HitSamples
                                ?.OrderBy(x => x.Time)
                                .Select(change => new SampleSetEvent(
                                    change.Time,
                                    NormalizeSampleSet(change.Sample, SampleSet.Normal),
                                    NormalizeSampleIndex(change.Index),
                                    NormalizeSampleVolume(change.Volume)))
                                .ToList()
                            ?? [];

        return new HitSoundTimeline
        {
            NonDraggableSoundTimeline = new SoundTimeline(nonDraggable),
            DraggableSoundTimeline = new SoundTimeline(draggable),
            SampleSetTimeline = new SampleSetTimeline
            {
                HitSamples = sampleChanges
            }
        };
    }

    private static SoundEvent NormalizeSoundEventForBeatmapEncoding(SoundEvent source)
    {
        var timeMs = Math.Max(0, source.Time.TotalMilliseconds);
        var normalSample = NormalizeSampleSet(source.NormalSample, SampleSet.Normal);
        var additionSample = NormalizeSampleSet(source.AdditionSample, normalSample);
        var fileName = string.IsNullOrWhiteSpace(source.FileName)
            ? string.Empty
            : Path.GetFileName(source.FileName.Trim().Trim('"'));

        return new SoundEvent(
            time: TimeSpan.FromMilliseconds(timeMs),
            hitSounds: NormalizeHitSoundsForBeatmapEncoding(source.HitSounds),
            normalSample: normalSample,
            additionSample: additionSample,
            fileName: fileName,
            sampleIndexOverride: NormalizeOptionalSampleIndex(source.SampleIndexOverride),
            sampleVolumeOverride: NormalizeOptionalSampleVolume(source.SampleVolumeOverride));
    }

    private static List<HitSound> NormalizeHitSoundsForBeatmapEncoding(IEnumerable<HitSound>? sourceHitSounds)
    {
        var combinedFlags = 0;
        if (sourceHitSounds is not null)
        {
            foreach (var hitSound in sourceHitSounds)
            {
                // The visualizer uses HitSound.Normal for lane identity.
                // Beatmap encoding expects "no additions" to be represented by HitSound.None.
                if (hitSound is HitSound.None or HitSound.Normal)
                {
                    continue;
                }

                combinedFlags |= (int)hitSound;
            }
        }

        var normalized = new List<HitSound>();
        if ((combinedFlags & (int)HitSound.Whistle) == (int)HitSound.Whistle)
        {
            normalized.Add(HitSound.Whistle);
        }

        if ((combinedFlags & (int)HitSound.Finish) == (int)HitSound.Finish)
        {
            normalized.Add(HitSound.Finish);
        }

        if ((combinedFlags & (int)HitSound.Clap) == (int)HitSound.Clap)
        {
            normalized.Add(HitSound.Clap);
        }

        if (normalized.Count == 0)
        {
            normalized.Add(HitSound.None);
        }

        return normalized;
    }

    private static int? NormalizeOptionalSampleIndex(int? sampleIndexOverride)
    {
        if (sampleIndexOverride is not > 0)
        {
            return null;
        }

        return Math.Clamp(sampleIndexOverride.Value, 1, 99);
    }

    private static int? NormalizeOptionalSampleVolume(int? sampleVolumeOverride)
    {
        if (sampleVolumeOverride is not > 0)
        {
            return null;
        }

        return Math.Clamp(sampleVolumeOverride.Value, 1, 100);
    }

    private static List<IHitObject> BuildCircleOnlyHitObjects(SoundTimeline soundTimeline)
    {
        var circles = new List<IHitObject>();
        var sortedEvents = soundTimeline.SoundEvents
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();

        for (var i = 0; i < sortedEvents.Count; i++)
        {
            var soundEvent = sortedEvents[i];
            var hitSample = new HitSample(
                normalSet: NormalizeSampleSet(soundEvent.NormalSample, SampleSet.Normal),
                additionSet: NormalizeSampleSet(soundEvent.AdditionSample, NormalizeSampleSet(soundEvent.NormalSample, SampleSet.Normal)),
                fileName: string.IsNullOrWhiteSpace(soundEvent.FileName)
                    ? string.Empty
                    : Path.GetFileName(soundEvent.FileName.Trim().Trim('"')),
                index: ToOptionalUnsignedInt(NormalizeOptionalSampleIndex(soundEvent.SampleIndexOverride)),
                volume: ToOptionalUnsignedInt(NormalizeOptionalSampleVolume(soundEvent.SampleVolumeOverride)));
            var hitSounds = NormalizeHitSoundsForBeatmapEncoding(soundEvent.HitSounds);
            var isNewCombo = i == 0;

            circles.Add(new Circle(
                coordinates: HitsoundDiffCirclePosition,
                time: soundEvent.Time,
                type: HitObjectType.Circle,
                hitSounds: (hitSample, hitSounds),
                newCombo: isNewCombo,
                comboOffset: 0));
        }

        return circles;
    }

    private static HitSoundCopierOptions BuildVisualizerExportOptions()
    {
        return new HitSoundCopierOptions
        {
            Leniency = VisualizerExportLeniencyMs,
            OverwriteEverything = true,
            OverwriteMuting = true,
            CopySliderBodySounds = false,
            CopySampleAndVolumeChanges = true
        };
    }

    private static uint? ToOptionalUnsignedInt(int? value)
    {
        return value is > 0 ? (uint)value.Value : null;
    }

    private static string BuildHitsoundDiffVersionName(string? sourceVersion)
    {
        var trimmed = string.IsNullOrWhiteSpace(sourceVersion)
            ? "Hitsound"
            : sourceVersion.Trim();

        if (trimmed.Contains("hitsound", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return $"{trimmed} (Hitsound)";
    }

    private static string BuildUniqueHitsoundDiffPath(string sourceBeatmapPath, string versionName)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceBeatmapPath) ?? string.Empty;
        var sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceBeatmapPath);
        var sanitizedVersionName = SanitizeFileNamePart(versionName);

        var bracketStartIndex = sourceFileNameWithoutExtension.LastIndexOf('[');
        var bracketEndIndex = sourceFileNameWithoutExtension.LastIndexOf(']');
        var baseFileName = bracketStartIndex >= 0 && bracketEndIndex > bracketStartIndex
            ? $"{sourceFileNameWithoutExtension[..(bracketStartIndex + 1)]}{sanitizedVersionName}{sourceFileNameWithoutExtension[bracketEndIndex..]}"
            : $"{sourceFileNameWithoutExtension} [{sanitizedVersionName}]";

        var candidatePath = Path.Combine(sourceDirectory, $"{baseFileName}.osu");
        if (!File.Exists(candidatePath))
        {
            return candidatePath;
        }

        var suffix = 1;
        while (true)
        {
            var suffixedPath = Path.Combine(sourceDirectory, $"{baseFileName} ({suffix}).osu");
            if (!File.Exists(suffixedPath))
            {
                return suffixedPath;
            }

            suffix++;
        }
    }

    private static string SanitizeFileNamePart(string value)
    {
        var sanitized = value;
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidCharacter, '_');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "Hitsound" : sanitized.Trim();
    }

    private static void WriteBeatmap(string path, Beatmap beatmap)
    {
        File.WriteAllText(path, beatmap.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));
    }
}
