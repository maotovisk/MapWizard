public static class PatternMatcher
{
    public static List<Pattern> Find(List<int> sequence)
    {
        List<Pattern> results = [];

        var index = 0;
        while (index < sequence.Count)
        {
            var pattern = Best(sequence, index);

            if (pattern == null)
            {
                index++;
                continue;
            }

            results.Add(pattern);
            index += pattern.Length;
        }

        return results;
    }

    private static Pattern? Best(List<int> sequence, int position)
    {
        Pattern? result = null;

        for (var lenght = 1; lenght + position <= sequence.Count; lenght++)
        {
            var pattern = sequence[position..(position + lenght)];
            var repetitions = Repetitions(sequence, position, pattern);

            if (result != null && repetitions * lenght > result.Repetitions * result.Length) continue;
            
            result = new(pattern, repetitions, (int)(repetitions * lenght), position);
        }

        return result;
    }

    private static double Repetitions(List<int> sequence, int patternIndex, List<int> pattern)
    {
        var position = patternIndex + pattern.Count;
        var repetitions = 0;

        while (position + pattern.Count <= sequence.Count)
        {
            if (!sequence[position..(position + pattern.Count)].SequenceEqual(pattern)) break;

            repetitions++;
            position += pattern.Count;
        }

        var partial = 0;

        for (var index = 0; index < pattern.Count && index + position < sequence.Count; index++)
        {
            if (sequence[position + index] != pattern[index]) break;

            partial++;
        }

        return repetitions + (double)partial / pattern.Count;
    }
}

public class Pattern
{
    // TODO: remove this
    public List<int> Sequence { get; } 
    
    public double Repetitions { get; }
    public int Length { get; }
    public int Position { get; }

    public Pattern(List<int> pattern, double repetitions, int length, int position)
    {
        Sequence = pattern;
        Repetitions = repetitions;
        Length = length;
        Position = position;
    }
}
