using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services;

public interface IOsuMemoryReaderService
{
    Result<string> GetBeatmapPath();
}