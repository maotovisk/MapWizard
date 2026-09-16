using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MapWizard.Desktop.Services;
using Velopack;

namespace MapWizard.Desktop.Views.Dialogs;

public partial class UpdateAvailableDialog : UserControl
{
    public UpdateAvailableDialog()
    {
        InitializeComponent();
    }

    public void Configure(UpdateInfo updateInfo, string currentVersionLabel)
    {
        var release = updateInfo.TargetFullRelease;

        VersionLine.Text = $"Version {release.Version} is available.";
        CurrentVersionLine.Text = $"You are currently running {currentVersionLabel}.";
        ChangelogMarkdown.Markdown = "Loading release notes…";

        if (UpdateService.IsTestFlowEnabled)
        {
            ChangelogMarkdown.Markdown = BuildSimulatedChangelog(release.Version.ToFullString());
            return;
        }

        _ = LoadGithubNotesAsync(release.Version.ToFullString());
    }

    private static string BuildSimulatedChangelog(string version) => $"""
        **This is a simulated release ({version})** for the `MAPWIZARD_UPDATE_FLOW_TESTING` update-flow test.

        - Simulated release-cycle check, modal and download progress
        - Notification toast entrance/exit animations
        - BusyArea fade-in/fade-out

        Clicking **Update** runs the simulated download; *no real files are touched*.
        """;

    private async Task LoadGithubNotesAsync(string version)
    {
        var notes = await GithubReleaseNotesService.GetNotesAsync(version);
        Dispatcher.UIThread.Post(() =>
        {
            ChangelogMarkdown.Markdown = string.IsNullOrWhiteSpace(notes)
                ? "No GitHub release notes were found for this version."
                : notes;
        });
    }
}
