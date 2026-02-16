using BeatmapParser.Colours;

namespace MapWizard.Tools.ComboColourStudio;

public class ComboColourProject
{
    public List<ComboColour> ComboColours { get; set; } = [];
    public List<ComboColourPoint> ColourPoints { get; set; } = [];
    public int MaxBurstLength { get; set; } = 1;
}
