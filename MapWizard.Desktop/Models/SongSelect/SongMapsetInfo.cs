using System;
using System.Collections.Generic;

namespace MapWizard.Desktop.Models.SongSelect;

public sealed record SongMapsetInfo
{
    public required string MapsetDirectoryPath { get; init; }
    public required string Artist { get; init; }
    public required string Title { get; init; }
    public required string Creator { get; init; }
    public required string? BackgroundImagePath { get; init; }
    public required DateTime LastEditUtc { get; init; }
    public required IReadOnlyList<SongDifficultyInfo> Difficulties { get; init; }
}
