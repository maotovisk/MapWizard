using System.Collections.Generic;
using System.Linq;

namespace MapWizard.Desktop.Services.HitsoundService;

public enum HitSoundTimingCompatibilityKind
{
    Compatible,
    OffsetOnlyMismatch,
    TimingMismatch
}

public sealed class HitSoundTimingCompatibilityTargetResult
{
    public required string TargetPath { get; init; }
    public required HitSoundTimingCompatibilityKind Kind { get; init; }
    public int SuggestedLeniencyMs { get; init; }
    public double OffsetMs { get; init; }
    public string? Details { get; init; }
}

public sealed class HitSoundTimingCompatibilityReport
{
    public required IReadOnlyList<HitSoundTimingCompatibilityTargetResult> Targets { get; init; }

    public bool HasOffsetOnlyMismatch => Targets.Any(x => x.Kind == HitSoundTimingCompatibilityKind.OffsetOnlyMismatch);
    public bool HasTimingMismatch => Targets.Any(x => x.Kind == HitSoundTimingCompatibilityKind.TimingMismatch);

    public int SuggestedLeniencyMs => Targets
        .Where(x => x.Kind == HitSoundTimingCompatibilityKind.OffsetOnlyMismatch)
        .Select(x => x.SuggestedLeniencyMs)
        .DefaultIfEmpty(0)
        .Max();

    public int OffsetOnlyMismatchCount => Targets.Count(x => x.Kind == HitSoundTimingCompatibilityKind.OffsetOnlyMismatch);
    public int TimingMismatchCount => Targets.Count(x => x.Kind == HitSoundTimingCompatibilityKind.TimingMismatch);
}
