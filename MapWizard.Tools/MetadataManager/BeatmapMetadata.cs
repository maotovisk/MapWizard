using System.Drawing;
using MapWizard.BeatmapParser;

namespace MapWizard.Tools.MetadataManager;

public class BeatmapMetadata
{
    public string Title { get; set; } = string.Empty;
    public string RomanizedTitle { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string RomanizedArtist { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int BeatmapId { get; set; } = 0;
    public int BeatmapSetId { get; set; } = -1;
    public string AudioFilename { get; set; }  = string.Empty;
    public string BackgroundFilename { get; set; } = string.Empty;
    public string VideoFilename { get; set; } = string.Empty;
    public int VideoOffset { get; set; } = 0;
    public int PreviewTime { get; set; } = -1;
    public List<ComboColour> Colours { get; set; } = [];
    public Color? SliderTrackColour { get; set; }
    public Color? SliderBorderColour { get; set; }
    public bool WidescreenStoryboard { get; set; } = false;
    public bool LetterboxInBreaks { get; set; } = false;
    public bool EpilepsyWarning { get; set; } = false;
    public bool SamplesMatch { get; set; } = false;
}