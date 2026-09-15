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
        _ = LoadGithubNotesAsync(release.Version.ToFullString());
    }

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
