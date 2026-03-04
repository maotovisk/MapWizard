using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services.MemoryService;

public interface IOsuMemoryReaderService
{
    Result<string> GetBeatmapPath();
    Result<int> GetCurrentTimestamp();
}
