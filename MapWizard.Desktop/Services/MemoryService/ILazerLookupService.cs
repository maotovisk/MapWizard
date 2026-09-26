using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services.MemoryService;

public interface ILazerLookupService
{
    Result<LazerSessionState> GetSessionState();

    /// <summary>
    /// Cheaper variant of <see cref="GetSessionState"/> for background polling: the process list is
    /// only inspected when an external-edit mount exists, so <see cref="LazerSessionState.IsRunning"/>
    /// is <see langword="false"/> whenever nothing is mounted.
    /// </summary>
    Result<LazerSessionState> GetMountedSessionState();
}
