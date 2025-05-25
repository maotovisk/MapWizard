namespace MapWizard.CLI;

public static class PatternMatcher
{
    /// <summary>
    /// Finds all repeated patterns (with at least 2 repetitions)
    /// and returns a list of Pattern(Position, Sequence, Repetitions),
    /// advancing in blocks to avoid overlapping.
    /// </summary>
    public static List<Pattern> Find(List<int> seq)
    {
        var results = new List<Pattern>();
        int i = 0;

        while (i < seq.Count)
        {
            var (pattern, reps) = FindBestAtPosition(seq, i);
            
            if (reps < 2)
            {
                i++;
            }
            else
            {
                results.Add(new Pattern(pattern, reps, i));
                i += pattern.Count * reps;
            }
        }

        return results;
    }

    /// <summary>
    /// For a given start position, tests all pattern lengths
    /// from 1 to half of the remaining sequence, and returns the one that
    /// maximizes (reps * length).
    /// </summary>
    private static (List<int> pattern, int repetitions) FindBestAtPosition(List<int> seq, int start)
    {
        int bestCoverage = 0;
        List<int> bestPattern = null!;
        int bestReps = 0;

        int remaining = seq.Count - start;
        int maxPatternLen = remaining;

        for (int len = 1; len <= maxPatternLen; len++)
        {
            var pat = seq.GetRange(start, len);

            int reps = 1;
            while (start + (reps + 1) * len <= seq.Count
                   && seq
                       .GetRange(start + reps * len, len)
                       .SequenceEqual(pat))
            {
                reps++;
            }

            if (reps >= 2)
            {
                int coverage = reps * len;
                if (coverage > bestCoverage)
                {
                    bestCoverage = coverage;
                    bestPattern = pat;
                    bestReps = reps;
                }
            }
        }

        if (bestPattern == null)
            return (new List<int> { seq[start] }, 1);

        return (bestPattern, bestReps);
    }
}

// Pattern.cs
public class Pattern
{
    /// <summary>
    /// The unit pattern, without repetitions.
    /// </summary>
    public List<int> Sequence { get; }

    /// <summary>
    /// How many times this pattern appears consecutively.
    /// </summary>
    public int Repetitions { get; }

    /// <summary>
    /// Initial index of this pattern in the original sequence.
    /// </summary>
    public int Position { get; }

    public Pattern(List<int> sequence, int repetitions, int position)
    {
        Sequence = sequence;
        Repetitions = repetitions;
        Position = position;
    }

    /// <summary>
    /// How many elements in total this repeated block covers.
    /// </summary>
    public int MatchedLength => Sequence.Count * Repetitions;
}
