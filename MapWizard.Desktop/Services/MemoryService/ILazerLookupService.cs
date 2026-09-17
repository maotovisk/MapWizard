using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services.MemoryService;

public interface ILazerLookupService
{
    Result<LazerSessionState> GetSessionState();
}
