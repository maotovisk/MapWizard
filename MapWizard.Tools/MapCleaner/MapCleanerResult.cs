namespace MapWizard.Tools.MapCleaner;

public class MapCleanerResult
{
    public int ObjectsResnapped;
    public int SliderEndsResnapped;
    public int SpinnerOrHoldEndsResnapped;
    public int BookmarksResnapped;
    public int GreenLinesResnapped;

    public int InheritedTimingPointsRemoved;
    public int HitSoundsRemoved;
    public int MutedTimingPointsRestored;
    public int UnclickableHitSoundsMuted;

    public MapCleanerAnalysis Analysis = new();
}

public class MapCleanerBatchResult
{
    public int ProcessedBeatmaps;
    public int FailedBeatmaps;
    public List<string> FailedPaths = [];
    public List<string> FailureDetails = [];

    public int ObjectsResnapped;
    public int SliderEndsResnapped;
    public int SpinnerOrHoldEndsResnapped;
    public int BookmarksResnapped;
    public int GreenLinesResnapped;

    public int InheritedTimingPointsRemoved;
    public int HitSoundsRemoved;
    public int MutedTimingPointsRestored;
    public int UnclickableHitSoundsMuted;

    public void Add(MapCleanerResult result)
    {
        ObjectsResnapped += result.ObjectsResnapped;
        SliderEndsResnapped += result.SliderEndsResnapped;
        SpinnerOrHoldEndsResnapped += result.SpinnerOrHoldEndsResnapped;
        BookmarksResnapped += result.BookmarksResnapped;
        GreenLinesResnapped += result.GreenLinesResnapped;
        InheritedTimingPointsRemoved += result.InheritedTimingPointsRemoved;
        HitSoundsRemoved += result.HitSoundsRemoved;
        MutedTimingPointsRestored += result.MutedTimingPointsRestored;
        UnclickableHitSoundsMuted += result.UnclickableHitSoundsMuted;
    }
}
