using System.Data.SqlTypes;
using System.Drawing;
using MapWizard.BeatmapParser;
using MapWizard.CLI.ConsoleUtils;
using MapWizard.Services;
using MapWizard.Tools.HitSoundCopier;

namespace MapWizard.CLI;

static class Program
{
    static void Main(string[] args)
    {
        var sourcePathProvided = IoParser.ArgumentExists(args, "--source") || IoParser.ArgumentExists(args, "-s");

        if (!sourcePathProvided)
        {
            Console.WriteLine("Please provide a source path using the --source or -s argument.");
            return;
        }

        var sourcePath = IoParser.GetArgumentValue(args, "--source") ??
                         IoParser.GetArgumentValue(args, "-s") ?? throw new Exception("Source path not found.");

        var beatmapString = File.ReadAllText(sourcePath);

        var beatmap = Beatmap.Decode(beatmapString);

        var objectsCombo = beatmap.HitObjects.Objects
            .Where(x => x.NewCombo && x.Type != HitObjectType.Spinner)
            .ToList();
        
        var comboColours = beatmap.Colours?.Combos.ToList() ?? new List<ComboColour>();

        if (comboColours.Count == 0) return;

        var combosWithIndexes = new List<(TimeSpan Time, int Index)>();
        int lastComboIndex = -1;
        foreach (var obj in objectsCombo)
        {
            var comboOffset = (int)obj.ComboOffset;
            var comboIndex = (lastComboIndex + 1 + comboOffset) % comboColours.Count;
            combosWithIndexes.Add((obj.Time, comboIndex));
            lastComboIndex = comboIndex;
        }
        
        var combos = combosWithIndexes.Select(x => x.Index).ToList();
        var patterns = PatternMatcher.Find(combos);

        var groupedPatterns = patterns
            .Select(p => (
                StartTime: combosWithIndexes[p.Position].Time,
                ColourIndexes: p.Sequence  // apenas o padrão unitário
            ))
            .ToList();

        Console.WriteLine($"{comboColours.Count} combo colours found.");
        Console.WriteLine($"{groupedPatterns.Count} grouped patterns.\n");

        foreach (var gp in groupedPatterns)
        {
            var numbers = gp.ColourIndexes
                .Select(idx => comboColours[idx].Number);
            Console.WriteLine(
                $"{gp.StartTime:hh\\:mm\\:ss\\.fff} - {string.Join(" ", numbers)}");
        }
    }
}