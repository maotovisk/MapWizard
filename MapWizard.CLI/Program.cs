using BeatmapParser;
using BeatmapParser.Enums;
using MapWizard.CLI.ConsoleUtils;
using MapWizard.CLI.Services;
using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Tools.HitSounds.Extensions;
using MapWizard.Tools.MapCleaner;

namespace MapWizard.CLI;

static class Program
{
    static void Main(string[] args)
    {
        var mode = args.GetArgumentValue("--mode") ?? args.GetArgumentValue("-m");
        
        if (mode == null)
        {
            Console.WriteLine("Please provide a mode using the --mode or -m argument.");
            return;
        }

        switch (mode)
        {
            case "copy":
                var sourcePath = args.GetArgumentValue("--source") ?? args.GetArgumentValue("-s");
                var targetPath = args.GetArgumentValue("--target") ?? args.GetArgumentValue("-t");

                if (sourcePath == null)
                {
                    Console.WriteLine("Please provide a source path using the --source or -s argument.");
                    return;
                }
    
                if (targetPath == null)
                {
                    Console.WriteLine("Please provide a target path using the --target or -t argument.");
                    return;
                }

                var hsOptions = new HitSoundCopierOptions
                {
                    CopyUsedSamplesIfDifferentMapset = args.ArgumentExists("--copy-used-samples")
                                                       || args.ArgumentExists("--copy-used-samples-if-different-mapset")
                };

                HsCopy(sourcePath, targetPath, hsOptions);
                break;
            case "info":
            {
                var path = args.GetArgumentValue("--path") ?? args.GetArgumentValue("-p");
            
                if (path == null)
                {
                    Console.WriteLine("Please provide a path using the --path or -p argument.");
                    return;
                }
            
                MapInfo(path);
                break;
            }
            case "clean":
            {
                var target = args.GetArgumentValue("--path") ??
                             args.GetArgumentValue("-p") ??
                             args.GetArgumentValue("--target") ??
                             args.GetArgumentValue("-t");

                if (target == null)
                {
                    Console.WriteLine("Please provide a target path using --path/-p or --target/-t.");
                    return;
                }

                var targetPaths = ParseTargetPaths(target);

                var snapsRaw = args.GetArgumentValue("--snaps") ?? "1/8,1/12";
                var options = new MapCleanerOptions
                {
                    SnapDivisors = ParseCsv(snapsRaw),
                    ResnapEverything = !args.ArgumentExists("--no-resnap"),
                    RemoveMuting = args.ArgumentExists("--remove-muting"),
                    RemoveUnusedGreenlines = args.ArgumentExists("--remove-unused-greenlines")
                };

                RunMapCleaner(targetPaths, options);
                break;
            }
        }
        
    }

    private static void RunMapCleaner(string[] targetPaths, MapCleanerOptions options)
    {
        var mapCleanerService = new MapCleanerService();
        var result = mapCleanerService.CleanMaps(targetPaths, options);

        if (result.FailedBeatmaps == 0)
        {
            Console.WriteLine(
                $"Map cleaner finished. Cleaned {result.ProcessedBeatmaps} beatmap(s): " +
                $"{result.TimingPointsResnapped} timing points resnapped, " +
                $"{result.ObjectsResnapped} object starts resnapped, " +
                $"{result.SliderEndsResnapped} slider ends resnapped, " +
                $"{result.GreenLinesRemoved} greenlines removed.");
            return;
        }

        Console.WriteLine(
            $"Map cleaner finished with issues. Cleaned {result.ProcessedBeatmaps} beatmap(s), " +
            $"{result.FailedBeatmaps} failed.");

        if (result.FailedPaths.Count > 0)
        {
            Console.WriteLine("Failed paths:");
            foreach (var failedPath in result.FailedPaths)
            {
                Console.WriteLine($"- {failedPath}");
            }
        }

        if (result.FailureDetails.Count > 0)
        {
            Console.WriteLine("Failure details:");
            foreach (var detail in result.FailureDetails)
            {
                Console.WriteLine($"- {detail}");
            }
        }
    }

    private static string[] ParseTargetPaths(string rawInput)
    {
        return rawInput
            .Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<string> ParseCsv(string rawInput)
    {
        return rawInput
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void MapInfo(string path)
    {
        var sourceBeatmap = Beatmap.Decode(File.ReadAllText(path));

        if (sourceBeatmap.GeneralSection.Mode == Ruleset.Mania)
        {
            var maniaMappedSamples = sourceBeatmap.MapManiaSoundsToSamples();
            
            foreach (var (fileName, sampleData ) in maniaMappedSamples)
            {
                Console.WriteLine($"Mapped file '{fileName}' to SampleSet '{sampleData.sampleSet}' and HitSound '{sampleData.sound}'. with name '{sampleData.ConvertToSampleName()}'");
            }
        }
    }

    private static void HsCopy(string sourcePath, string targetPath, HitSoundCopierOptions options)
    {
        var hitSoundService = new HitSoundService();
        
        hitSoundService.CopyHitsounds(sourcePath, targetPath, options);
        
        Console.WriteLine("Hitsounds copied successfully!");
    }
}
