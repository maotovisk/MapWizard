using MapWizard.Tools.Results;

namespace MapWizard.Tools.HitSounds;

public static class HitSoundErrors
{
    public static readonly Error ManiaNotSupported = new(
        "hitsounds.mania_not_supported",
        "Hit Sound copying for mania is currently WIP and not supported yet.");

    public static Error FileNotFound(string path) => new(
        "hitsounds.file_not_found",
        $"Beatmap file '{path}' does not exist.");
}
