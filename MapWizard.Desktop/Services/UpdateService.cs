using System;
using System.Linq;
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
    private const string SimulatedVersion = "999.0.0";
    private const string SimulatedVersionLabel = "999.0.0-simulated";

    public static readonly bool IsTestFlowEnabled =
        Environment.GetEnvironmentVariable("MAPWIZARD_UPDATE_FLOW_TESTING") == "1";

    // Simulated release-cycle bookkeeping (MAPWIZARD_UPDATE_FLOW_TESTING=1 only).
    private bool _simulatedUpdateDownloaded;
    private bool _simulatedUpdateApplied;

    public bool IsInstalled => IsTestFlowEnabled || CreateUpdateManager().IsInstalled;
    public bool IsRestartRequired => _simulatedUpdateDownloaded && !_simulatedUpdateApplied;

    public string VersionLabel
    {
        get
        {
            if (IsTestFlowEnabled)
            {
                return _simulatedUpdateApplied
                    ? SimulatedVersionLabel
                    : "0.0.3-local-simulated";
            }

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
        if (IsTestFlowEnabled)
        {
            return Task.FromResult<UpdateInfo?>(BuildSimulatedUpdateInfo());
        }

        var updateManager = CreateUpdateManager();
        if (!updateManager.IsInstalled)
        {
            return Task.FromResult<UpdateInfo?>(null);
        }

        return updateManager.CheckForUpdatesAsync();
    }

    public async Task DownloadUpdatesAsync(
        UpdateInfo updateInfo,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (IsTestFlowEnabled)
        {
            progress ??= static _ => { };
            // Simulate a release-cycle download: chunked percentage ticks at
            // driver-adjusted pacing so UI progress paths can be exercised.
            const int ChunkCount = 12;
            var chunks = Enumerable.Range(1, ChunkCount)
                .Select(i => (int)Math.Ceiling(i * 100d / ChunkCount));
            foreach (var percent in chunks)
            {
                await Task.Delay(250, cancellationToken).ConfigureAwait(true);
                progress(percent);
            }
            _simulatedUpdateDownloaded = true;
            return;
        }

        var updateManager = CreateUpdateManager();
        await updateManager.DownloadUpdatesAsync(updateInfo, progress ?? (_ => { }), cancellationToken);
    }

    public bool RestartToApplyPendingUpdate()
    {
        if (IsTestFlowEnabled)
        {
            if (!_simulatedUpdateDownloaded || _simulatedUpdateApplied)
            {
                return false;
            }

            // No real restart while simulating: mark applied and keep running.
            _simulatedUpdateApplied = true;
            return true;
        }

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
        if (IsTestFlowEnabled)
        {
            _simulatedUpdateApplied = true;
            return;
        }

        CreateUpdateManager().WaitExitThenApplyUpdates(updateInfo);
    }

    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
    {
        if (IsTestFlowEnabled)
        {
            _simulatedUpdateApplied = true;
            return;
        }

        CreateUpdateManager().ApplyUpdatesAndRestart(updateInfo);
    }

    private static UpdateInfo BuildSimulatedUpdateInfo()
    {
        var asset = new VelopackAsset
        {
            PackageId = "MapWizard",
            Version = SemanticVersion.Parse(SimulatedVersion)
        };
        return new UpdateInfo(asset, isDowngrade: false);
    }

    private UpdateManager CreateUpdateManager()
    {
        var includePrereleases = CurrentStream == UpdateStream.PreRelease;
        var source = new GithubSource(RepositoryUrl, null, includePrereleases, null);
        return new UpdateManager(source);
    }
}
