namespace MapWizard.Tools.MapCleaner;

public class MapCleanerOptions
{
    public List<string> SnapDivisors = ["1/8", "1/12"];

    public bool ResnapEverything = true;

    public bool RemoveMuting;
    public bool RemoveUnusedGreenlines;

    public int ForwardRedlineWindowMs = 10;
}
