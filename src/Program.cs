using MapWizard.BeatmapParser;

namespace MapWizard;

class Program
{
    static void Main(string[] args)
    {
        var inputFile = "C:/Users/Maot/projects/HitsoundCopier/test5.osu";

        var fileToParse = File.ReadAllText(inputFile);

        var beatmap = Beatmap.Decode(fileToParse);

        Console.WriteLine(beatmap.Encode());
    }

}