namespace MapWizard.Tools.MapCleaner;

public class MapCleanerResult
{
    public int TimingPointsResnapped;
    public int ObjectsResnapped;
    public int SliderEndsResnapped;
    public int SpinnerOrHoldEndsResnapped;
    public int BookmarksResnapped;
    public int PreviewTimeResnapped;
    public int GreenLinesResnapped;

    public int GreenLinesRemoved;
    public int MutedTimingPointsRestored;
}

public class MapCleanerBatchResult
{
    public int ProcessedBeatmaps;
    public int FailedBeatmaps;
    public List<string> FailedPaths = [];
    public List<string> FailureDetails = [];

    public int TimingPointsResnapped;
    public int ObjectsResnapped;
    public int SliderEndsResnapped;
    public int SpinnerOrHoldEndsResnapped;
    public int BookmarksResnapped;
    public int PreviewTimeResnapped;
    public int GreenLinesResnapped;

    public int GreenLinesRemoved;
    public int MutedTimingPointsRestored;

    public void Add(MapCleanerResult result)
    {
        TimingPointsResnapped += result.TimingPointsResnapped;
        ObjectsResnapped += result.ObjectsResnapped;
        SliderEndsResnapped += result.SliderEndsResnapped;
        SpinnerOrHoldEndsResnapped += result.SpinnerOrHoldEndsResnapped;
        BookmarksResnapped += result.BookmarksResnapped;
        PreviewTimeResnapped += result.PreviewTimeResnapped;
        GreenLinesResnapped += result.GreenLinesResnapped;
        GreenLinesRemoved += result.GreenLinesRemoved;
        MutedTimingPointsRestored += result.MutedTimingPointsRestored;
    }
}
