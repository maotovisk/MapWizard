using MapWizard.Tools.MapCleaner;

namespace MapWizard.Desktop.Services.MapCleanerService;

public interface IMapCleanerService
{
    public bool CleanMaps(string[] targetPaths, MapCleanerOptions options, out MapCleanerBatchResult result);
}
