namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "DarkMode")]
    public bool DarkMode { get; set; } = false;

    [Setting("General", "SongsPath")]
    public string SongsPath { get; set; } = string.Empty;

    [Setting("General", "UpdateStream")]
    public UpdateStream UpdateStream { get; set; } = UpdateStream.Release;
}
