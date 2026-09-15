using Avalonia.Controls;
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
        ChangelogText.Text = string.IsNullOrWhiteSpace(release.NotesMarkdown)
            ? "No release notes were provided for this version."
            : release.NotesMarkdown;
    }
}
