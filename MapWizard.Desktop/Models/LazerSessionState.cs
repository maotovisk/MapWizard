using System.Collections.Generic;

namespace MapWizard.Desktop.Models;

public sealed record LazerSessionState(
    bool IsRunning,
    IReadOnlyList<string> MountedBeatmapPaths);
