using MapWizard.Tools.MapCleaner;

namespace MapWizard.CLI.Services;

public class MapCleanerService
{
    public MapCleanerBatchResult CleanMaps(string[] targetPaths, MapCleanerOptions options)
    {
        return MapCleaner.CleanBeatmapTargets(targetPaths, options);
    }
}
