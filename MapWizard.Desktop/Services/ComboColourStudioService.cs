using System.Collections.Generic;
using System.Drawing;
using MapWizard.Tools.ComboColourStudio;

namespace MapWizard.Desktop.Services;

public class ComboColourStudioService : IComboColourStudioService
{
    public ComboColourProject ImportComboColours(string beatmapPath)
    {
        return ComboColourStudio.ImportComboColoursFromBeatmap(beatmapPath);
    }

    public ComboColourProject ExtractColourHax(string beatmapPath, int maxBurstLength)
    {
        return ComboColourStudio.ExtractColourHaxFromBeatmap(beatmapPath, maxBurstLength);
    }

    public void ApplyProject(ComboColourProject project, string[] targetPaths, ComboColourStudioOptions options)
    {
        ComboColourStudio.ApplyProjectToBeatmaps(project, targetPaths, options);
    }

    public string? GetBackgroundPath(string beatmapPath)
    {
        return ComboColourStudio.GetBeatmapBackgroundPath(beatmapPath);
    }

    public List<Color> GenerateProminentColours(string imagePath, int maxColours)
    {
        return ComboColourStudio.GenerateProminentColours(imagePath, maxColours);
    }
}
