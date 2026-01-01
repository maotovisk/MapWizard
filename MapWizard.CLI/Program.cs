using MapWizard.CLI.ConsoleUtils;
using MapWizard.CLI.Services;
using MapWizard.Services;
using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.CLI;

static class Program
{
    static void Main(string[] args)
    {
        // Create a new instance of the HitSoundService
        var hitSoundService = new HitSoundService();
        
        // Check if the user has provided the source and target paths
        var sourcePathProvided = IoParser.ArgumentExists(args, "--source") || IoParser.ArgumentExists(args, "-s");
        var targetPathProvided = IoParser.ArgumentExists(args, "--target") || IoParser.ArgumentExists(args, "-t");

        if (!sourcePathProvided)
        {
            Console.WriteLine("Please provide a source path using the --source or -s argument.");
            return;
        }
        
        if (!targetPathProvided)
        {
            Console.WriteLine("Please provide a target path using the --target or -t argument.");
            return;
        }
        
        // Copy the hitsounds from the source path to the target path
        
        var sourcePath = IoParser.GetArgumentValue(args, "--source") ?? IoParser.GetArgumentValue(args, "-s") ?? throw new Exception("Source path not found.");
        var targetPath = IoParser.GetArgumentValue(args, "--target") ?? IoParser.GetArgumentValue(args, "-t") ?? throw new Exception("Target path not found.");
        
        hitSoundService.CopyHitsounds(sourcePath, targetPath, new HitSoundCopierOptions());
        
        Console.WriteLine("Hitsounds copied successfully!");
    }
}