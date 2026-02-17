using System;

namespace MapWizard.Desktop.Models.SongSelect;

public sealed record SongDifficultyInfo
{
    public required string Name { get; init; }
    public required string OsuFilePath { get; init; }
    public required DateTime LastEditUtc { get; init; }
}
