using System;
using System.Threading;
using System.Threading.Tasks;
using MapWizard.Desktop.Models.Settings;
using Velopack;

namespace MapWizard.Desktop.Services;

public interface IUpdateService
{
    bool IsInstalled { get; }
    bool IsRestartRequired { get; }
    string VersionLabel { get; }
    UpdateStream CurrentStream { get; }
    void SetUpdateStream(UpdateStream stream);
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progress = null, CancellationToken cancellationToken = default);
    bool RestartToApplyPendingUpdate();
    void WaitExitThenApplyUpdates(UpdateInfo updateInfo);
    void ApplyUpdatesAndRestart(UpdateInfo updateInfo);
}
