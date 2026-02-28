using MapWizard.Tools.MapCleaner;

namespace MapWizard.Desktop.Services.MapCleanerService;

public interface IMapCleanerService
{
    public bool TryAnalyzeMap(string beatmapPath, out MapCleanerAnalysis analysis);
    public bool CleanMaps(string[] targetPaths, MapCleanerOptions options, out MapCleanerBatchResult result);
}
