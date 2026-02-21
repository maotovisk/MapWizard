namespace MapWizard.Tools.MapCleaner;

public class MapCleanerOptions
{
    public List<string> SnapDivisors = ["1/8", "1/12"];

    public bool AnalyzeSamples = true;
    public bool ResnapObjects = true;
    public bool ResnapSliderEnds = true;
    public bool ResnapGreenLines = true;
    public bool ResnapBookmarks;

    public bool RemoveUnusedInheritedTimingPoints = true;
    public bool RemoveHitSounds;
    public bool RemoveUnusedSamples;
    public bool RemoveMuting;
    public bool MuteUnclickableHitsounds;

    public int RedlineLookaheadForObjectsMs = 10;
    public int RedlineLookaheadForEndsMs = 20;
}
