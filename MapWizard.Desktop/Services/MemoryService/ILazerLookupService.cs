using System.Collections.Generic;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services.MemoryService;

public interface ILazerLookupService
{
    Result<IReadOnlyList<string>> GetMountedBeatmapPaths();
}
