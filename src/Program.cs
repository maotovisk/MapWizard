using MapWizard.BeatmapParser;

namespace MapWizard;

class Program
{
    static void Main(string[] args)
    {
        var inputFile = "/home/maot/projects/HitsoundCopier/test5.osu";

        var fileToParse = File.ReadAllText(inputFile);

        var beatmap = Beatmap.Decode(fileToParse);

        var beatmapString = beatmap.Encode();

        File.WriteAllText("test5-parsed.osu", beatmapString);
    }
}