using System.Collections.Generic;
using System.Drawing;
using MapWizard.Tools.ComboColourStudio;

namespace MapWizard.Desktop.Services;

public interface IComboColourStudioService
{
    ComboColourProject ImportComboColours(string beatmapPath);
    ComboColourProject ExtractColourHax(string beatmapPath, int maxBurstLength);
    void ApplyProject(ComboColourProject project, string[] targetPaths, ComboColourStudioOptions options);
    string? GetBackgroundPath(string beatmapPath);
    List<Color> GenerateProminentColours(string imagePath, int maxColours);
}
