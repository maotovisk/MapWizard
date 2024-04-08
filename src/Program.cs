using MapWizard.BeatmapParser;

namespace MapWizard;

class Program
{
    static void Main(string[] args)
    {
        var inputFile = "/home/maot/projects/HitsoundCopier/test4.osu";

        var fileToParse = File.ReadAllText(inputFile);

        var beatmap = Beatmap.Decode(fileToParse);

        var beatmapString = beatmap.Encode();

        File.WriteAllText("test4-parsed.osu", beatmapString);
    }
}