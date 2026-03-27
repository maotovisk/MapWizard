using System;
using MapWizard.Tools.MapCleaner;

namespace MapWizard.Desktop.Services.MapCleanerService;

public class MapCleanerService : IMapCleanerService
{
    public bool CleanMaps(string[] targetPaths, MapCleanerOptions options, out MapCleanerBatchResult result)
    {
        result = new MapCleanerBatchResult();

        try
        {
            result = MapCleaner.CleanBeatmapTargets(targetPaths, options);
            return result.FailedBeatmaps == 0;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}
