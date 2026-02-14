using System;
using System.Threading;
using System.Threading.Tasks;
using MapWizard.Desktop.Models.Settings;
using Velopack;
using Velopack.Sources;

namespace MapWizard.Desktop.Services;

public class UpdateService(ISettingsService settingsService) : IUpdateService
{
    private const string RepositoryUrl = "https://github.com/maotovisk/MapWizard";
    private const string LocalDevVersionLabel = "MapWizard-localdev";

    public bool IsInstalled => CreateUpdateManager().IsInstalled;
    public bool IsRestartRequired => CreateUpdateManager().UpdatePendingRestart != null;

    public string VersionLabel
    {
        get
        {
            var updateManager = CreateUpdateManager();
            return updateManager.IsInstalled
                ? updateManager.CurrentVersion?.ToFullString() ?? LocalDevVersionLabel
                : LocalDevVersionLabel;
        }
    }

    public UpdateStream CurrentStream => settingsService.GetMainSettings().UpdateStream;

    public void SetUpdateStream(UpdateStream stream)
    {
        var settings = settingsService.GetMainSettings();
        if (settings.UpdateStream == stream)
        {
            return;
        }

        settings.UpdateStream = stream;
        settingsService.SaveMainSettings(settings);
    }

    public Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        var updateManager = CreateUpdateManager();
        if (!updateManager.IsInstalled)
        {
            return Task.FromResult<UpdateInfo?>(null);
        }

        return updateManager.CheckForUpdatesAsync();
    }

    public Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        var updateManager = CreateUpdateManager();
        return updateManager.DownloadUpdatesAsync(updateInfo, progress ?? (_ => { }), cancellationToken);
    }

    public bool RestartToApplyPendingUpdate()
    {
        var updateManager = CreateUpdateManager();
        var pendingUpdate = updateManager.UpdatePendingRestart;
        if (pendingUpdate == null)
        {
            return false;
        }

        updateManager.ApplyUpdatesAndRestart(pendingUpdate);
        return true;
    }

    public void WaitExitThenApplyUpdates(UpdateInfo updateInfo)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);
        CreateUpdateManager().WaitExitThenApplyUpdates(updateInfo);
    }

    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);
        CreateUpdateManager().ApplyUpdatesAndRestart(updateInfo);
    }

    private UpdateManager CreateUpdateManager()
    {
        var includePrereleases = CurrentStream == UpdateStream.PreRelease;
        var source = new GithubSource(RepositoryUrl, null, includePrereleases, null);
        return new UpdateManager(source);
    }
}
