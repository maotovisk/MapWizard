using MapWizard.Tools.ComboColourStudio;

namespace MapWizard.Desktop.Services;

public interface IComboColourProjectStore
{
    ComboColourProject? TryLoadProject(string beatmapPath, int beatmapId);
    void SaveProject(string beatmapPath, int beatmapId, ComboColourProject project);
}
