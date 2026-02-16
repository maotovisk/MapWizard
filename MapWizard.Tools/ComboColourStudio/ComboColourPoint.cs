namespace MapWizard.Tools.ComboColourStudio;

public class ComboColourPoint
{
    public double Time { get; set; }
    public List<int> ColourSequence { get; set; } = [];
    public ColourPointMode Mode { get; set; } = ColourPointMode.Normal;
}
