using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatmapParser;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.Desktop.Services;

public class HitSoundService : IHitSoundService
{
    private const double RedlineTimeToleranceMs = 0.5d;
    private const double BeatLengthToleranceMs = 0.001d;

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

    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPaths, HitSoundCopierOptions options)
    {
        try
        {
            HitSoundCopier.CopyFromBeatmapToTarget(sourcePath: sourcePath, targetPath: targetPaths, options: options);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
        
        return true;
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
}
