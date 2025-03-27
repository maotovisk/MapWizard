using System.Data.SqlTypes;
using System.Drawing;
using MapWizard.BeatmapParser;
using MapWizard.CLI.ConsoleUtils;
using MapWizard.Services;
using MapWizard.Tools.HitSoundCopier;

namespace MapWizard.CLI;

class Program
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
        
        var objectManuallySet = objectsCombo
            .Where(x => x.ComboOffset != 0)
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
            Console.WriteLine(comboIndex);
        }
        
        var groupedPatterns = new List<(TimeSpan StartTime, List<int> ColourIndexes)>();
        TimeSpan? groupStartTime = null;
        List<int> currentPattern = new();
        List<int> previousPattern = new();
        foreach (var (time, index) in combosWithIndexes)
        {
            groupStartTime ??= time;

            var comboOffset = objectsCombo.Find(x => x.Time == time).ComboOffset;
            
            var combosWasManuallySet = comboOffset != 0;

            if (currentPattern.Count == 1 && combosWasManuallySet)
            {
                groupedPatterns.Add(((TimeSpan)groupStartTime, currentPattern));
                previousPattern = currentPattern;
                currentPattern = [];
                groupStartTime = time;
            }
            
            // we can verify if we want to group the pattern
            // by looking at the combo offset at the original object list at the right time
            // if the combo offset is zero we can group the pattern into one sequence
            if (!currentPattern.Contains(index))
            {
                if (currentPattern.Count > 0 && currentPattern.SequenceEqual(previousPattern))
                {
                    groupedPatterns.Add(((TimeSpan)groupStartTime, currentPattern));
                    previousPattern = currentPattern;
                    currentPattern = [];
                    groupStartTime = time;
                }
                currentPattern.Add(index);
            }
            else
            {
                if (combosWasManuallySet)
                {
                    groupedPatterns.Add(((TimeSpan)groupStartTime, currentPattern));
                    previousPattern = currentPattern;
                    currentPattern = [index];
                    groupStartTime = time;
                }
                else
                {
                    currentPattern.Add(index);
                } 
            }
        }
        
        if (currentPattern.Count > 0)
        {
            groupedPatterns.Add(((TimeSpan)groupStartTime, currentPattern));
        }
        var lastPattern = groupedPatterns.FirstOrDefault();
        
        foreach (var comboIndexSequence in groupedPatterns.ToList())
        {
            var pastPatterns =  groupedPatterns.Where(x => x.StartTime < comboIndexSequence.StartTime).ToList();
         
            if (comboIndexSequence.ColourIndexes.Count == 1)
            {
                var partternCandidate = HasPatternCandidate(lastPattern.ColourIndexes, comboIndexSequence.ColourIndexes[0], pastPatterns);
                
                if (partternCandidate.result)
                {
                    // we need to find the start time of the pattern that was returned, to do that, we need to find the object that was used to create the pattern
                    var indexToLookFor = partternCandidate.pattern[0];
                    var patternStartTime = combosWithIndexes.OrderBy(x=> x.Time).Where(x => x.Index == indexToLookFor && x.Time > lastPattern.StartTime).Select(x => x.Time).FirstOrDefault();
                    if (patternStartTime != default)
                    {
                        groupedPatterns[groupedPatterns.IndexOf(lastPattern)] = (lastPattern.StartTime, lastPattern.ColourIndexes.Slice(0, comboIndexSequence.ColourIndexes[0]));
                        groupedPatterns.Insert(groupedPatterns.IndexOf(comboIndexSequence), (patternStartTime, partternCandidate.pattern));
                    }
                    groupedPatterns.Remove(comboIndexSequence);
                }
            }
            
            lastPattern = comboIndexSequence;
        }
        
        //filter repeating sequenced patterns
        List<(TimeSpan StartTime, List<int> ColourIndexes)> filteredPatterns = new();

        foreach (var comboSequence in groupedPatterns)
        {
            if (filteredPatterns.Count == 0)
            {
                filteredPatterns.Add(comboSequence);
                continue;
            }

            var lastOne = filteredPatterns.Last();
            if (lastOne.ColourIndexes.SequenceEqual(comboSequence.ColourIndexes))
            {
                continue;
            }

            filteredPatterns.Add(comboSequence);
        }
        
        Console.WriteLine($"{comboColours.Count} combo colours found.");
        Console.WriteLine($"{groupedPatterns.Count} grouped patterns.");
        Console.WriteLine();
        Console.WriteLine("Grouped pattern:");
        groupedPatterns.ForEach(x =>
        {
            Console.WriteLine(
                $"{x.StartTime.ToString()} - {string.Join(" ", x.ColourIndexes.Select(c => comboColours[c].Number))}");
        });
    }

    static (bool result, List<int> pattern) HasPatternCandidate(List<int> lastComboSequence, int candidate,
        List<(TimeSpan StartTime, List<int> ColourIndexes)> previousPatterns)
    {
        if (previousPatterns.Count == 0) return (false, []);
        
        var lastPattern = previousPatterns.Last();
        
        if (lastPattern.ColourIndexes.Count == 1 && lastPattern.ColourIndexes[0] == candidate)
        {
            return (true, lastPattern.ColourIndexes);
        }
        
        if (!lastPattern.ColourIndexes.Contains(candidate))
        {
            return (false, []);
        }
        
        // split the list of pattern in the candidateIndex with the candidate so we can search for other patterns that are being formed
        var splitPatternWithCandidate =
            lastComboSequence.Slice(lastComboSequence.IndexOf(candidate), lastComboSequence.Count - lastComboSequence.IndexOf(candidate));
        
        if (previousPatterns.Any(x => x.ColourIndexes.SequenceEqual(splitPatternWithCandidate)))
        {
            return (true, splitPatternWithCandidate);
        }

        return (false, []);
    }

    static (bool result, (TimeSpan StartTime, List<int> pattern) foundMatch) HasPatternCandidate(List<int> lastComboSequence, List<int> patternCandidate, List<(TimeSpan StartTime, List<int> ColourIndexes)> previousPatterns)
    {
        if (previousPatterns.Count == 0) 
            return (false, (TimeSpan.Zero,[]));
        
        var validPatterns = previousPatterns.Where(x => x.ColourIndexes.Count >= patternCandidate.Count).ToList();

        foreach (var validPattern in validPatterns)
        {
            if (validPattern.ColourIndexes.Count == 1 && validPattern.ColourIndexes[0] == patternCandidate[0])
            {
                return (true, validPattern);
            }
            
            if (!validPattern.ColourIndexes.Contains(patternCandidate[0]))
            {
                continue;
            }
            
            // check if validPattern.ColourIndexes contains the patternCandidate
            var patternFound = validPattern.ColourIndexes
                .Slice(validPattern.ColourIndexes.IndexOf(patternCandidate[0]), validPattern.ColourIndexes.Count - validPattern.ColourIndexes.IndexOf(patternCandidate[0]));
            
            if (patternFound.Count == patternCandidate.Count && patternFound.SequenceEqual(patternCandidate))
            {
                return (true, validPattern);
            }
        }

        return (false, (TimeSpan.Zero,[]));
    }
}