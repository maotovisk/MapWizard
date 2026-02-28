namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "ThemeMode")]
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    [Setting("General", "SongsPath")]
    public string SongsPath { get; set; } = string.Empty;

    [Setting("General", "UpdateStream")]
    public UpdateStream UpdateStream { get; set; } = UpdateStream.Release;
}
