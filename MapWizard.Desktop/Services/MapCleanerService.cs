using System;
using System.IO;
using BeatmapParser;
using MapWizard.Tools.MapCleaner;

namespace MapWizard.Desktop.Services;

public class MapCleanerService : IMapCleanerService
{
    public bool TryAnalyzeMap(string beatmapPath, out MapCleanerAnalysis analysis)
    {
        analysis = new MapCleanerAnalysis();

        try
        {
            if (string.IsNullOrWhiteSpace(beatmapPath) || !File.Exists(beatmapPath))
            {
                return false;
            }

            var beatmap = Beatmap.Decode(File.ReadAllText(beatmapPath));
            analysis = MapCleaner.AnalyzeBeatmap(beatmap);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

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
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}
