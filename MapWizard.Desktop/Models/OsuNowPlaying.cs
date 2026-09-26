using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace MapWizard.Desktop.Models;

public enum OsuClient
{
    Stable,
    Lazer
}

/// <summary>
/// Snapshot of the beatmap currently open in an osu! client. Instances are immutable; a new one is
/// published whenever the open beatmap changes. The owner disposes <see cref="Background"/> once the
/// snapshot is replaced.
/// </summary>
public sealed class OsuNowPlaying(
    OsuClient client,
    string mapsetDirectory,
    IReadOnlyList<string> beatmapPaths,
    string artist,
    string title,
    string? difficulty,
    string creator,
    Bitmap? background) : IDisposable
{
    public OsuClient Client { get; } = client;
    public string MapsetDirectory { get; } = mapsetDirectory;

    /// <summary>
    /// The open difficulty for stable; every difficulty of the mounted set for lazer, which does not
    /// expose which one is being edited.
    /// </summary>
    public IReadOnlyList<string> BeatmapPaths { get; } = beatmapPaths;

    public string Artist { get; } = artist;
    public string Title { get; } = title;
    public string? Difficulty { get; } = difficulty;
    public string Creator { get; } = creator;
    public Bitmap? Background { get; } = background;

    public bool HasBackground => Background is not null;
    public string ClientLabel => Client == OsuClient.Lazer ? "osu!lazer" : "osu!stable";

    public string Headline => Difficulty is null ? $"{Artist} - {Title}" : $"{Artist} - {Title} [{Difficulty}]";

    public string Byline => Client == OsuClient.Lazer && BeatmapPaths.Count > 1
        ? $"mapped by {Creator} · {BeatmapPaths.Count} difficulties"
        : $"mapped by {Creator}";

    public string ToolTip => $"{Headline}\n{Byline}\n{ClientLabel}";

    public void Dispose() => Background?.Dispose();
}
