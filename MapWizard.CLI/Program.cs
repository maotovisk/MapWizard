using MapWizard.CLI.ConsoleUtils;
using MapWizard.Services;

namespace MapWizard.CLI;

class Program
{
    static void Main(string[] args)
    {   
        // Create a new instance of the HitSoundService
        var hitSoundService = new HitSoundService();
        
        // Check if the user has provided the source and target paths
        var sourcePath = IoParser.ArgumentExists(args, "--source") || IoParser.ArgumentExists(args, "-s");
        var targetPath = IoParser.ArgumentExists(args, "--target") || IoParser.ArgumentExists(args, "-t");

        if (!sourcePath)
        {
            Console.WriteLine("Please provide a source path using the --source or -s argument.");
            return;
        }
        
        if (!targetPath)
        {
            Console.WriteLine("Please provide a target path using the --target or -t argument.");
            return;
        }
        
        // Copy the hitsounds from the source path to the target path
        
        hitSoundService.CopyHitsounds(IoParser.GetArgumentValue(args, "--source") ?? IoParser.GetArgumentValue(args, "-s") ?? throw new Exception("Source path not found."), IoParser.GetArgumentValue(args, "--target") ?? IoParser.GetArgumentValue(args, "-t") ?? throw new Exception("Target path not found."));
        
        Console.WriteLine("Hitsounds copied successfully!");
    }
}