using BeatmapParser;
using BeatmapParser.Enums;
using MapWizard.CLI.ConsoleUtils;
using MapWizard.CLI.Services;
using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Tools.HitSounds.Extensions;

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
            
                HsCopy(sourcePath, targetPath);
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
        }
        
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

    private static void HsCopy(string sourcePath, string targetPath)
    {
        var hitSoundService = new HitSoundService();
        
        hitSoundService.CopyHitsounds(sourcePath, targetPath, new HitSoundCopierOptions());
        
        Console.WriteLine("Hitsounds copied successfully!");
    }
}